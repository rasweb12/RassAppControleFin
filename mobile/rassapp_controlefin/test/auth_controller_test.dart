import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:rassapp_controlefin/core/network/api_client.dart';
import 'package:rassapp_controlefin/core/security/session_store.dart';
import 'package:rassapp_controlefin/features/auth/auth_controller.dart';
import 'package:rassapp_controlefin/features/auth/auth_session.dart';

void main() {
  test(
    'renews an expiring session once for concurrent protected requests',
    () async {
      var renewals = 0;
      final store = MemorySessionStore(expiringSession());
      final controller = AuthController(
        ApiClient(
          client: MockClient((request) async {
            expect(request.url.path, '/api/v1/auth/renew');
            renewals++;
            return http.Response(envelope(freshSession()), HttpStatus.ok);
          }),
        ),
        store,
      );
      await controller.restore();

      final headers = await Future.wait([
        controller.authorizationHeaders(tenantId: 'tenant-a'),
        controller.authorizationHeaders(tenantId: 'tenant-a'),
      ]);

      expect(renewals, 1);
      expect(headers.first['Authorization'], 'Bearer fresh-access');
      expect(store.value?.accessToken, 'fresh-access');
    },
  );

  test('logout clears local state and requests remote revocation', () async {
    final store = MemorySessionStore(AuthSession.fromJson(freshSession()));
    final controller = AuthController(
      ApiClient(
        client: MockClient((request) async {
          expect(request.url.path, '/api/v1/auth/sign-out');
          final payload = jsonDecode(request.body) as Map<String, dynamic>;
          expect(payload['sessionId'], 'session-id');
          return http.Response(
            '{"success":true,"data":null,"error":null}',
            HttpStatus.ok,
          );
        }),
      ),
      store,
    );
    await controller.restore();

    final result = await controller.signOut();

    expect(result.remoteRevoked, isTrue);
    expect(controller.isAuthenticated, isFalse);
    expect(store.value, isNull);
  });

  test('renews once and retries a protected read after an expired token response',
      () async {
    var tenantReads = 0;
    var renewals = 0;
    final store = MemorySessionStore(AuthSession.fromJson(freshSession()));
    final controller = AuthController(
      ApiClient(
        client: MockClient((request) async {
          if (request.url.path == '/api/v1/auth/renew') {
            renewals++;
            return http.Response(envelope(freshSession()), HttpStatus.ok);
          }

          expect(request.url.path, '/api/v1/tenants');
          tenantReads++;
          if (tenantReads == 1) {
            expect(request.headers['authorization'], 'Bearer fresh-access');
            return http.Response(
              '{"success":false,"data":null,"error":{"message":"Sessao expirada."}}',
              HttpStatus.unauthorized,
            );
          }

          expect(request.headers['authorization'], 'Bearer fresh-access');
          return http.Response(envelope(<String, dynamic>{}), HttpStatus.ok);
        }),
      ),
      store,
    );
    await controller.restore();

    await controller.getAuthenticated('/api/v1/tenants');

    expect(tenantReads, 2);
    expect(renewals, 1);
  });

  test('does not retry a financial post after an unauthorized response', () async {
    var posts = 0;
    final store = MemorySessionStore(AuthSession.fromJson(freshSession()));
    final controller = AuthController(
      ApiClient(
        client: MockClient((request) async {
          if (request.url.path == '/api/v1/auth/renew') {
            fail('A mutacao nao deve ser renovada nem repetida automaticamente.');
          }

          posts++;
          return http.Response(
            '{"success":false,"data":null,"error":{"message":"Sessao expirada."}}',
            HttpStatus.unauthorized,
          );
        }),
      ),
      store,
    );
    await controller.restore();

    await expectLater(
      controller.postAuthenticated('/api/v1/transactions', const {'amount': 10}),
      throwsA(
        isA<ApiException>().having(
          (exception) => exception.failure,
          'failure',
          ApiFailure.unauthorized,
        ),
      ),
    );

    expect(posts, 1);
  });
}

class MemorySessionStore extends SessionStore {
  MemorySessionStore(this.value);
  AuthSession? value;

  @override
  Future<AuthSession?> read() async => value;

  @override
  Future<void> save(AuthSession session) async => value = session;

  @override
  Future<void> clear() async => value = null;
}

AuthSession expiringSession() => AuthSession(
  sessionId: 'session-id',
  accessToken: 'old-access',
  sessionToken: 'session-token',
  subject: 'subject-id',
  accessExpiresAt: DateTime.now().toUtc().add(const Duration(seconds: 10)),
  sessionExpiresAt: DateTime.now().toUtc().add(const Duration(hours: 1)),
);

Map<String, dynamic> freshSession() => {
  'sessionId': 'session-id',
  'accessToken': 'fresh-access',
  'sessionToken': 'fresh-session-token',
  'subject': 'subject-id',
  'accessExpiresAt': DateTime.now()
      .toUtc()
      .add(const Duration(minutes: 15))
      .toIso8601String(),
  'sessionExpiresAt': DateTime.now()
      .toUtc()
      .add(const Duration(hours: 1))
      .toIso8601String(),
};

String envelope(Map<String, dynamic> data) =>
    jsonEncode({'success': true, 'data': data, 'error': null});
