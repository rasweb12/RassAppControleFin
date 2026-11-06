import 'dart:convert';
import 'dart:io';
import 'package:rassapp_controlefin/features/synchronization/finance_sync_service.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:drift/native.dart';
import 'package:rassapp_controlefin/core/network/api_client.dart';
import 'package:rassapp_controlefin/core/storage/offline_database.dart';
import 'package:rassapp_controlefin/core/storage/offline_finance_store.dart';
import 'package:rassapp_controlefin/features/auth/auth_controller.dart';
import 'package:rassapp_controlefin/features/auth/auth_session.dart';
import 'package:rassapp_controlefin/features/finance/finance_repository.dart';
import 'auth_controller_test.dart' show MemorySessionStore, freshSession;

void main() {
  late OfflineDatabase database;
  late OfflineFinanceStore store;
  setUp(() {
    database = OfflineDatabase(NativeDatabase.memory());
    store = OfflineFinanceStore.forTesting(database, subject: 'subject-id');
  });
  tearDown(() => database.close());

  Future<FinanceRepository> repository(MockClient client) async {
    final auth = AuthController(
      ApiClient(client: client),
      MemorySessionStore(AuthSession.fromJson(freshSession())),
    );
    await auth.restore();
    return FinanceRepository(auth, store: Future.value(store));
  }

  test(
    'offline goal edit preserves its version and conflict is not resent',
    () async {
      var offline = true;
      var puts = 0;
      final repo = await repository(
        MockClient((request) async {
          if (request.method == 'PUT') {
            puts++;
            final body = jsonDecode(request.body) as Map<String, dynamic>;
            expect(body['version'], 7);
            if (offline) throw const SocketException('synthetic offline');
            return http.Response(
              '{"success":false,"data":null,"error":{"message":"Conflito de versão."}}',
              409,
            );
          }
          return http.Response(
            '{"success":true,"data":{"changes":[],"nextCursor":null,"appliedCursor":null},"error":null}',
            200,
          );
        }),
      );
      final result = await repo.updateGoal(
        tenantId: 'tenant-a',
        goal: const GoalSummary(
          id: 'goal-a',
          name: 'Sintética',
          targetAmount: 200,
          currentAmount: 41,
          currency: 'BRL',
          targetDate: null,
          status: 'Active',
          version: 7,
        ),
        name: 'Edição offline',
        targetAmount: 250,
      );
      expect(result, FinanceWriteState.pending);
      final pending = await store.pending('tenant-a');
      expect(pending, hasLength(1));
      expect(pending.single.payload['version'], 7);
      expect(pending.single.operationType, 'put');
      offline = false;
      expect(await repo.synchronize('tenant-a'), FinanceSyncResult.blocked);
      expect(await store.conflicts('tenant-a'), hasLength(1));
      await repo.synchronize('tenant-a');
      expect(puts, 2);
      expect(await store.pending('tenant-b'), isEmpty);
    },
  );

  test(
    'requests selected window and tenant and preserves monetary results',
    () async {
      final repo = await repository(
        MockClient((request) async {
          expect(request.method, 'GET');
          expect(request.url.queryParameters['months'], '12');
          expect(request.headers['X-Tenant-Id'], 'tenant-a');
          return http.Response(
            jsonEncode({
              'success': true,
              'data': [
                {
                  'categoryId': 'category-a',
                  'category': 'Alimentação',
                  'currency': 'BRL',
                  'recentExpense': 120.50,
                  'previousExpense': 100,
                  'increase': 20.50,
                  'suggestedReduction': 20.50,
                },
              ],
              'error': null,
            }),
            200,
          );
        }),
      );
      final rows = await repo.expenseReductionOpportunities(
        'tenant-a',
        months: 12,
      );
      expect(rows.single.recentExpense, 120.50);
      expect(rows.single.suggestedReduction, 20.50);
      expect(await store.pendingCount('tenant-a'), 0);
    },
  );

  test(
    'rejected window remains an explicit failure without queued mutation',
    () async {
      final repo = await repository(
        MockClient(
          (request) async => http.Response(
            jsonEncode({
              'success': false,
              'data': null,
              'error': {'message': 'Período inválido.'},
            }),
            400,
          ),
        ),
      );
      await expectLater(
        repo.expenseReductionOpportunities('tenant-a', months: 13),
        throwsA(isA<ApiException>()),
      );
      expect(await store.pendingCount('tenant-a'), 0);
    },
  );

  test(
    'goal edit sends optimistic version and preserves civil target date',
    () async {
      final repo = await repository(
        MockClient((request) async {
          expect(request.method, 'PUT');
          expect(request.url.path, '/api/v1/goals/goal-a');
          final body = jsonDecode(request.body) as Map<String, dynamic>;
          expect(body['version'], 7);
          expect(body['targetDate'], '2026-12-05T12:00:00.000Z');
          expect(body.containsKey('currentAmount'), false);
          return http.Response('{"success":true,"data":{},"error":null}', 200);
        }),
      );
      final result = await repo.updateGoal(
        tenantId: 'tenant-a',
        goal: const GoalSummary(
          id: 'goal-a',
          name: 'Reserva',
          targetAmount: 100,
          currentAmount: 40,
          currency: 'BRL',
          targetDate: null,
          status: 'Active',
          version: 7,
        ),
        name: 'Reserva revisada',
        targetAmount: 200,
        targetDate: DateTime(2026, 12, 5),
      );
      expect(result, FinanceWriteState.applied);
    },
  );
}
