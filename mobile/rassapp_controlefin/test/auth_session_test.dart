import 'package:flutter_test/flutter_test.dart';
import 'package:rassapp_controlefin/features/auth/auth_session.dart';

void main() {
  test('auth session round-trips its API contract', () {
    final session = AuthSession.fromJson({
      'sessionId': '2c8d9b3c-74f3-40f3-91a5-96701b3ce952',
      'accessToken': 'access',
      'sessionToken': 'session',
      'subject': 'user-1',
      'accessExpiresAt': '2026-10-12T12:00:00Z',
      'sessionExpiresAt': '2026-10-13T12:00:00Z',
    });
    expect(session.toJson()['subject'], 'user-1');
    expect(session.sessionExpiresAt.isAfter(session.accessExpiresAt), isTrue);
  });
}
