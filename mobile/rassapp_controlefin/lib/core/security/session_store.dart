import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../features/auth/auth_session.dart';

class SessionStore {
  static const _key = 'rassapp.auth-session.v1';
  final FlutterSecureStorage _storage = const FlutterSecureStorage();

  Future<AuthSession?> read() async {
    final value = await _storage.read(key: _key);
    if (value == null) return null;
    try {
      final session = AuthSession.fromJson(
        jsonDecode(value) as Map<String, dynamic>,
      );
      if (session.sessionExpiresAt.isBefore(DateTime.now().toUtc())) {
        await clear();
        return null;
      }
      return session;
    } on Object {
      await clear();
      return null;
    }
  }

  Future<void> save(AuthSession session) =>
      _storage.write(key: _key, value: jsonEncode(session.toJson()));
  Future<void> clear() => _storage.delete(key: _key);
}
