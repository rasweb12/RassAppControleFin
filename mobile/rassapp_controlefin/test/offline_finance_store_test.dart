import 'dart:io';

import 'package:drift/native.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/testing.dart';
import 'package:rassapp_controlefin/core/network/api_client.dart';
import 'package:rassapp_controlefin/core/storage/offline_database.dart';
import 'package:rassapp_controlefin/core/storage/offline_finance_store.dart';
import 'package:rassapp_controlefin/core/security/session_store.dart';
import 'package:rassapp_controlefin/features/auth/auth_controller.dart';
import 'package:rassapp_controlefin/features/auth/auth_session.dart';
import 'package:rassapp_controlefin/features/finance/finance_repository.dart';
import 'package:sqlite3/sqlite3.dart' as sqlite;

void main() {
  test(
    'isolates cached records and pending operations by subject and tenant',
    () async {
      final database = OfflineDatabase(NativeDatabase.memory());
      final alice = OfflineFinanceStore.forTesting(database, subject: 'alice');
      final bob = OfflineFinanceStore.forTesting(database, subject: 'bob');

      await alice.cacheOne('tenant-a', 'account', {
        'id': 'account-a',
        'name': 'Conta da Alice',
        'version': 1,
      });
      await alice.enqueue(
        tenantId: 'tenant-a',
        endpoint: '/api/v1/accounts',
        payload: const {'operationId': 'operation-1', 'name': 'Nova conta'},
        entityType: 'account',
        entityId: 'local-operation-1',
        operationType: 'create',
      );

      expect(await alice.cached('tenant-a', 'account'), hasLength(1));
      expect(await bob.cached('tenant-a', 'account'), isEmpty);
      expect(await alice.pendingCount('tenant-a'), 1);
      expect(await bob.pendingCount('tenant-a'), 0);

      await alice.acknowledge('operation-1');
      expect(await alice.pendingCount('tenant-a'), 0);
      await database.close();
    },
  );

  test('keeps the same operation id while retries are deferred', () async {
    final database = OfflineDatabase(NativeDatabase.memory());
    final store = OfflineFinanceStore.forTesting(database, subject: 'alice');

    await store.enqueue(
      tenantId: 'tenant-a',
      endpoint: '/api/v1/transactions',
      payload: const {'operationId': 'stable-operation'},
      entityType: 'transaction',
      entityId: 'local-stable-operation',
      operationType: 'create',
    );
    await store.defer(
      'stable-operation',
      safeError: 'Sem conexão.',
      retryable: true,
    );

    final stored = await database
        .select(database.pendingSyncOperations)
        .getSingle();
    expect(stored.operationId, 'stable-operation');
    expect(stored.retryCount, 1);
    expect(await store.pending('tenant-a'), isEmpty);
    await database.close();
  });

  test(
    'keeps conflicts isolated until the user explicitly discards them',
    () async {
      final database = OfflineDatabase(NativeDatabase.memory());
      final store = OfflineFinanceStore.forTesting(database, subject: 'alice');
      await store.enqueue(
        tenantId: 'tenant-a',
        endpoint: '/api/v1/transfers',
        payload: const {'operationId': 'conflicting-operation'},
        entityType: 'transfer',
        entityId: 'local-conflicting-operation',
        operationType: 'create',
      );
      await store.defer(
        'conflicting-operation',
        safeError: 'Conflito que exige revisão.',
        retryable: false,
      );

      final conflicts = await store.conflicts('tenant-a');
      expect(conflicts, hasLength(1));
      expect(conflicts.single.entityType, 'transfer');
      expect(await store.pendingCount('tenant-a'), 0);

      await store.discardConflict('tenant-a', 'conflicting-operation');
      expect(await store.conflicts('tenant-a'), isEmpty);
      await database.close();
    },
  );

  test('isolates sync cursors and deduplicates inbox operations', () async {
    final database = OfflineDatabase(NativeDatabase.memory());
    final alice = OfflineFinanceStore.forTesting(database, subject: 'alice');
    final bob = OfflineFinanceStore.forTesting(database, subject: 'bob');

    await alice.setCursor('tenant-a', 'cursor-a');
    await alice.rememberInbox('tenant-a', ['operation-1', 'operation-1']);
    await bob.rememberInbox('tenant-a', ['operation-1']);

    expect(await alice.cursor('tenant-a'), 'cursor-a');
    expect(await bob.cursor('tenant-a'), isNull);

    final inbox = await database.select(database.syncInbox).get();
    expect(inbox, hasLength(2));
    expect(inbox.map((item) => item.operationId), everyElement('operation-1'));
    expect(inbox.map((item) => item.subject), containsAll(['alice', 'bob']));
    await database.close();
  });

  test(
    'applies a remote page, records its inbox marker, and advances cursor atomically',
    () async {
      final database = OfflineDatabase(NativeDatabase.memory());
      final store = OfflineFinanceStore.forTesting(database, subject: 'alice');
      await store.cacheOne('tenant-a', 'account', {
        'id': 'local-pending-account',
        'name': 'Conta pendente',
        'version': 0,
      });
      await store.cacheOne('tenant-a', 'account', {
        'id': 'account-server',
        'name': 'Valor desatualizado',
        'version': 1,
      });

      await store.applyRemotePage(
        tenantId: 'tenant-a',
        changes: const [
          RemoteSyncChange(
            operationId: 'remote-operation-1',
            resourceId: 'account-server',
            eventType: 'OpenAccountCommand',
          ),
        ],
        snapshots: {
          'account': [
            {'id': 'account-server', 'name': 'Conta atualizada', 'version': 2},
          ],
        },
        appliedCursor: 'cursor-remote-1',
      );

      final accounts = await store.cached('tenant-a', 'account');
      expect(accounts, hasLength(2));
      expect(
        accounts.singleWhere((item) => item['id'] == 'account-server')['name'],
        'Conta atualizada',
      );
      expect(
        accounts.singleWhere((item) => item['id'] == 'local-pending-account'),
        isNotNull,
      );
      expect(await store.cursor('tenant-a'), 'cursor-remote-1');
      final inbox = await database.select(database.syncInbox).get();
      expect(inbox, hasLength(1));
      expect(inbox.single.operationId, 'remote-operation-1');
      await database.close();
    },
  );

  test(
    'upgrades the inbox primary key without losing legacy records',
    () async {
      final directory = await Directory.systemTemp.createTemp(
        'rass-offline-db-',
      );
      final databaseFile = File(
        '${directory.path}${Platform.pathSeparator}app.db',
      );
      addTearDown(() => directory.delete(recursive: true));

      final legacy = sqlite.sqlite3.open(databaseFile.path);
      legacy.execute('''
      CREATE TABLE sync_inbox (
        operation_id TEXT NOT NULL PRIMARY KEY,
        subject TEXT NOT NULL,
        tenant_id TEXT NOT NULL,
        received_at INTEGER NOT NULL
      )
    ''');
      legacy.execute('''
      INSERT INTO sync_inbox (operation_id, subject, tenant_id, received_at)
      VALUES ('legacy-operation', 'alice', 'tenant-a', 0)
    ''');
      legacy.execute('PRAGMA user_version = 1');
      legacy.close();

      final database = OfflineDatabase(NativeDatabase(databaseFile));
      addTearDown(database.close);
      final inbox = await database.select(database.syncInbox).get();

      expect(inbox, hasLength(1));
      expect(inbox.single.operationId, 'legacy-operation');
      expect(inbox.single.subject, 'alice');
    },
  );

  test(
    'queues an offline income and updates the local dashboard snapshot',
    () async {
      final database = OfflineDatabase(NativeDatabase.memory());
      final store = OfflineFinanceStore.forTesting(database, subject: 'alice');
      await store.cacheOne('tenant-a', 'account', {
        'id': 'account-a',
        'name': 'Conta',
        'currency': 'BRL',
        'balance': 100,
        'version': 1,
        'isArchived': false,
      });
      await store.cacheOne('tenant-a', 'dashboard', {
        'id': 'BRL',
        'currency': 'BRL',
        'netWorth': 100,
        'income': 0,
        'expense': 0,
        'savingsRate': 0,
        'budgetUsed': 0,
        'activeGoals': 0,
      });

      final controller = AuthController(
        ApiClient(
          client: MockClient((_) => throw const SocketException('offline')),
        ),
        _MemorySessionStore(_session()),
      );
      await controller.restore();
      final repository = FinanceRepository(
        controller,
        store: Future.value(store),
      );

      final state = await repository.createTransaction(
        tenantId: 'tenant-a',
        account: AccountSummary(
          id: 'account-a',
          name: 'Conta',
          currency: 'BRL',
          balance: 100,
          version: 1,
          archived: false,
        ),
        category: CategorySummary(
          id: 'category-a',
          name: 'Salário',
          flow: 'Income',
          archived: false,
        ),
        amount: 50,
        description: 'Salário',
      );

      expect(state, FinanceWriteState.pending);
      expect(await store.pendingCount('tenant-a'), 1);
      expect(
        (await store.cached('tenant-a', 'account')).single['balance'],
        150,
      );
      final dashboard = (await store.cached('tenant-a', 'dashboard')).single;
      expect(dashboard['netWorth'], 150);
      expect(dashboard['income'], 50);
      expect(dashboard['savingsRate'], 1);
      await database.close();
    },
  );
}

class _MemorySessionStore extends SessionStore {
  _MemorySessionStore(this.value);
  AuthSession? value;

  @override
  Future<AuthSession?> read() async => value;

  @override
  Future<void> save(AuthSession session) async => value = session;

  @override
  Future<void> clear() async => value = null;
}

AuthSession _session() => AuthSession(
  sessionId: 'session-id',
  accessToken: 'access-token',
  sessionToken: 'session-token',
  subject: 'alice',
  accessExpiresAt: DateTime.now().toUtc().add(const Duration(minutes: 15)),
  sessionExpiresAt: DateTime.now().toUtc().add(const Duration(hours: 1)),
);
