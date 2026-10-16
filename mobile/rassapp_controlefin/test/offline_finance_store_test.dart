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
