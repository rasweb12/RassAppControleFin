import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:rassapp_controlefin/core/network/api_client.dart';

void main() {
  test('returns the data portion of a successful API envelope', () async {
    final client = ApiClient(
      client: MockClient(
        (request) async => http.Response(
          '{"success":true,"data":{"subject":"user-1"},"error":null}',
          HttpStatus.ok,
        ),
      ),
    );

    final result = await client.post('/api/v1/auth/sign-in', {
      'email': 'user@example.com',
      'password': 'password',
    });

    expect(result['subject'], 'user-1');
  });

  test('maps validation responses without exposing transport details', () async {
    final client = ApiClient(
      client: MockClient(
        (request) async => http.Response(
          '{"success":false,"data":null,"error":{"message":"Dados invalidos."}}',
          HttpStatus.badRequest,
        ),
      ),
    );

    expect(
      () => client.post('/api/v1/auth/sign-up', const {}),
      throwsA(
        isA<ApiException>()
            .having((error) => error.failure, 'failure', ApiFailure.validation)
            .having((error) => error.message, 'message', 'Dados invalidos.'),
      ),
    );
  });

  test('maps a duplicate registration response to conflict', () async {
    final client = ApiClient(
      client: MockClient(
        (request) async => http.Response(
          '{"success":false,"data":null,"error":{"code":"ACCOUNT_ALREADY_EXISTS","message":"Ja existe um cadastro para este e-mail."}}',
          HttpStatus.conflict,
        ),
      ),
    );

    expect(
      () => client.post('/api/v1/auth/sign-up', const {}),
      throwsA(
        isA<ApiException>().having(
          (error) => error.failure,
          'failure',
          ApiFailure.conflict,
        ),
      ),
    );
  });

  test(
    'sends authenticated finance headers without exposing them in results',
    () async {
      final client = ApiClient(
        client: MockClient((request) async {
          expect(request.headers['authorization'], 'Bearer access-token');
          expect(request.headers['x-app-session'], 'session-id');
          expect(request.headers['x-tenant-id'], 'tenant-id');
          expect(request.url.queryParameters['page'], '1');
          return http.Response(
            '{"success":true,"data":[],"error":null}',
            HttpStatus.ok,
          );
        }),
      );

      final result = await client.get(
        '/api/v1/accounts',
        headers: const {
          'Authorization': 'Bearer access-token',
          'X-App-Session': 'session-id',
          'X-Tenant-ID': 'tenant-id',
        },
        queryParameters: const {'page': '1'},
      );

      expect(result, isEmpty);
    },
  );

  test('maps network failures to an offline message', () async {
    final client = ApiClient(
      client: MockClient((request) => throw const SocketException('offline')),
    );

    expect(
      () => client.post('/api/v1/auth/sign-in', const {}),
      throwsA(
        isA<ApiException>()
            .having((error) => error.failure, 'failure', ApiFailure.offline)
            .having(
              (error) => error.message,
              'message',
              'Sem conexão com a internet.',
            ),
      ),
    );
  });

  test(
    'maps business and authorization failures without leaking transport details',
    () async {
      final client = ApiClient(
        client: MockClient(
          (request) async => http.Response(
            '{"success":false,"data":null,"error":{"message":"Operação não permitida."}}',
            HttpStatus.unprocessableEntity,
            headers: const {'x-correlation-id': 'correlation-1'},
          ),
        ),
      );

      expect(
        () => client.post('/api/v1/transactions', const {}),
        throwsA(
          isA<ApiException>()
              .having(
                (error) => error.failure,
                'failure',
                ApiFailure.ruleViolation,
              )
              .having(
                (error) => error.correlationId,
                'correlationId',
                'correlation-1',
              ),
        ),
      );
    },
  );

  test('treats malformed responses as contract failures', () async {
    final client = ApiClient(
      client: MockClient(
        (request) async => http.Response('{invalid', HttpStatus.ok),
      ),
    );

    expect(
      () => client.get('/api/v1/system'),
      throwsA(
        isA<ApiException>().having(
          (error) => error.failure,
          'failure',
          ApiFailure.unexpected,
        ),
      ),
    );
  });
}
