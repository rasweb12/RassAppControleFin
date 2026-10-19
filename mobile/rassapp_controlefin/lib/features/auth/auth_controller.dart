import 'package:flutter/foundation.dart';

import '../../core/network/api_client.dart';
import '../../core/security/session_store.dart';
import 'auth_session.dart';

class SignOutResult {
  const SignOutResult({required this.remoteRevoked});
  final bool remoteRevoked;
}

class AuthController extends ChangeNotifier {
  AuthController(this._api, this._store);
  final ApiClient _api;
  final SessionStore _store;
  AuthSession? _session;
  Future<AuthSession>? _renewal;
  bool _loading = true;
  int _generation = 0;

  bool get isLoading => _loading;
  bool get isAuthenticated => _session != null;
  AuthSession get session {
    final session = _session;
    if (session == null) {
      throw const ApiException(
        ApiFailure.unauthorized,
        'Sua sessão não é mais válida.',
      );
    }
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

  Future<void> requestPasswordReset(String email) async {
    await _api.postData('/api/v1/auth/request-password-reset', {
      'email': email,
    });
  }

  Future<void> _authenticate(String path, Map<String, dynamic> input) async {
    final session = AuthSession.fromJson(await _api.post(path, input));
    await _replaceSession(session, expectedGeneration: _generation);
  }

  Future<Object?> getAuthenticated(
    String path, {
    String? tenantId,
    Map<String, String>? queryParameters,
  }) async {
    final generation = _generation;
    try {
      return await _api.get(
        path,
        headers: await authorizationHeaders(tenantId: tenantId),
        queryParameters: queryParameters,
      );
    } on ApiException catch (error) {
      if (error.failure != ApiFailure.unauthorized ||
          generation != _generation) {
        rethrow;
      }
      await _sessionForRequest(forceRenew: true);
      if (generation != _generation) {
        throw const ApiException(
          ApiFailure.unauthorized,
          'Sua sessão foi encerrada.',
        );
      }
      return _api.get(
        path,
        headers: await authorizationHeaders(tenantId: tenantId),
        queryParameters: queryParameters,
      );
    }
  }

  Future<Object?> postAuthenticated(
    String path,
    Object? payload, {
    String? tenantId,
  }) => _postAuthenticated(path, payload, tenantId: tenantId);

  Future<Object?> patchAuthenticated(
    String path,
    Object? payload, {
    String? tenantId,
  }) async => _api.patchData(
    path,
    payload,
    headers: await authorizationHeaders(tenantId: tenantId),
  );

  Future<Object?> putAuthenticated(
    String path,
    Object? payload, {
    String? tenantId,
  }) async => _api.putData(
    path,
    payload,
    headers: await authorizationHeaders(tenantId: tenantId),
  );

  Future<Object?> _postAuthenticated(
    String path,
    Object? payload, {
    String? tenantId,
  }) async => _api.postData(
    path,
    payload,
    headers: await authorizationHeaders(tenantId: tenantId),
  );

  Future<Map<String, String>> authorizationHeaders({String? tenantId}) async {
    final session = await _sessionForRequest();
    return {
      'Authorization': 'Bearer ${session.accessToken}',
      'X-App-Session': session.sessionId,
      'X-Tenant-ID': ?tenantId,
    };
  }

  Future<AuthSession> _sessionForRequest({bool forceRenew = false}) async {
    final current = session;
    if (!forceRenew && !current.isAccessExpiring) return current;
    if (current.isSessionExpired) {
      await _clearLocalSession();
      throw const ApiException(
        ApiFailure.unauthorized,
        'Sua sessão expirou. Entre novamente.',
      );
    }
    final expectedGeneration = _generation;
    final renewal = _renewal ??= _renew(current, expectedGeneration);
    try {
      return await renewal;
    } finally {
      if (identical(_renewal, renewal)) _renewal = null;
    }
  }

  Future<AuthSession> _renew(
    AuthSession current,
    int expectedGeneration,
  ) async {
    try {
      final renewed = AuthSession.fromJson(
        await _api.post('/api/v1/auth/renew', {
          'sessionId': current.sessionId,
          'sessionToken': current.sessionToken,
        }),
      );
      if (expectedGeneration != _generation) {
        throw const ApiException(
          ApiFailure.unauthorized,
          'Sua sessão foi encerrada.',
        );
      }
      await _replaceSession(renewed, expectedGeneration: expectedGeneration);
      return renewed;
    } on ApiException catch (error) {
      if (error.failure == ApiFailure.unauthorized ||
          error.failure == ApiFailure.forbidden) {
        await _clearLocalSession();
      }
      rethrow;
    }
  }

  Future<SignOutResult> signOut() async {
    final previous = _session;
    _generation++;
    await _clearLocalSession();
    if (previous == null) return const SignOutResult(remoteRevoked: true);
    try {
      await _api.postData('/api/v1/auth/sign-out', {
        'sessionId': previous.sessionId,
        'sessionToken': previous.sessionToken,
      });
      return const SignOutResult(remoteRevoked: true);
    } on ApiException {
      return const SignOutResult(remoteRevoked: false);
    }
  }

  Future<void> _replaceSession(
    AuthSession session, {
    required int expectedGeneration,
  }) async {
    if (expectedGeneration != _generation) return;
    await _store.save(session);
    if (expectedGeneration != _generation) return;
    _session = session;
    notifyListeners();
  }

  Future<void> _clearLocalSession() async {
    _session = null;
    await _store.clear();
    notifyListeners();
  }
}
