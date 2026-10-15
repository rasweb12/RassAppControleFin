import 'package:flutter/foundation.dart';

import '../../core/network/api_client.dart';
import '../../core/security/session_store.dart';
import 'auth_session.dart';

class AuthController extends ChangeNotifier {
  AuthController(this._api, this._store);
  final ApiClient _api;
  final SessionStore _store;
  AuthSession? _session;
  bool _loading = true;

  bool get isLoading => _loading;
  bool get isAuthenticated => _session != null;
  ApiClient get api => _api;
  AuthSession get session {
    final session = _session;
    if (session == null) throw StateError('Não há sessão autenticada.');
    return session;
  }

  Future<void> restore() async {
    _session = await _store.read();
    _loading = false;
    notifyListeners();
  }

  Future<void> signIn(String email, String password) => _authenticate(
    '/api/v1/auth/sign-in',
    {'email': email, 'password': password},
  );
  Future<void> signUp(String name, String email, String password) =>
      _authenticate('/api/v1/auth/sign-up', {
        'name': name,
        'email': email,
        'password': password,
      });

  Future<void> _authenticate(String path, Map<String, dynamic> input) async {
    _session = AuthSession.fromJson(await _api.post(path, input));
    await _store.save(_session!);
    notifyListeners();
  }

  Future<void> signOut() async {
    _session = null;
    await _store.clear();
    notifyListeners();
  }
}
