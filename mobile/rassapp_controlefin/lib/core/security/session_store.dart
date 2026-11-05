import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../features/auth/auth_session.dart';

class SessionStore {
  static const _key = 'rassapp.auth-session.v1';
  final FlutterSecureStorage _storage = const FlutterSecureStorage();

  Future<AuthSession?> read() async {
    String? value;
    try {
      value = await _storage.read(key: _key);
    } on Object {
      // Do not expose encrypted content, keys, or platform exception details.
      debugPrint('RASS_SESSION_RESTORE status=storage-error');
      return null;
    }
    if (value == null) {
      debugPrint('RASS_SESSION_RESTORE status=missing');
      return null;
    }
    try {
      final session = AuthSession.fromJson(
        jsonDecode(value) as Map<String, dynamic>,
      );
      if (session.sessionExpiresAt.isBefore(DateTime.now().toUtc())) {
        debugPrint('RASS_SESSION_RESTORE status=expired');
        await clear();
        return null;
      }
      debugPrint('RASS_SESSION_RESTORE status=restored');
      return session;
    } on Object {
      debugPrint('RASS_SESSION_RESTORE status=malformed');
      await clear();
      return null;
    }
  }

  Future<void> save(AuthSession session) =>
      _storage.write(key: _key, value: jsonEncode(session.toJson()));
  Future<void> clear() => _storage.delete(key: _key);
}
