import 'package:connectivity_plus/connectivity_plus.dart';

import '../../core/network/api_client.dart';
import '../../core/storage/offline_finance_store.dart';
import '../auth/auth_controller.dart';

enum FinanceSyncResult { synchronized, offline, blocked, failed }

class FinanceSyncService {
  FinanceSyncService(this._auth, this._store, {Connectivity? connectivity})
    : _connectivity = connectivity ?? Connectivity();

  final AuthController _auth;
  final OfflineFinanceStore _store;
  final Connectivity _connectivity;
  bool _running = false;

  Future<FinanceSyncResult> synchronize(String tenantId) async {
    if (_running) return FinanceSyncResult.failed;
    _running = true;
    try {
      final state = await _connectivity.checkConnectivity();
      if (state.contains(ConnectivityResult.none)) {
        return FinanceSyncResult.offline;
      }

      var blocked = false;
      for (final operation in await _store.pending(tenantId)) {
        try {
          await _auth.postAuthenticated(
            operation.endpoint,
            operation.payload,
            tenantId: operation.tenantId,
          );
          await _store.acknowledge(operation.operationId);
        } on ApiException catch (error) {
          if (_isRetryable(error.failure)) {
            await _store.defer(
              operation.operationId,
              safeError: _safeError(error.failure),
              retryable: true,
            );
            return FinanceSyncResult.offline;
          }
          blocked = true;
          await _store.defer(
            operation.operationId,
            safeError: _safeError(error.failure),
            retryable: false,
          );
        }
      }
      return blocked
          ? FinanceSyncResult.blocked
          : FinanceSyncResult.synchronized;
    } finally {
      _running = false;
    }
  }

  bool _isRetryable(ApiFailure failure) => switch (failure) {
    ApiFailure.offline ||
    ApiFailure.timeout ||
    ApiFailure.unavailable ||
    ApiFailure.rateLimited => true,
    _ => false,
  };

  String _safeError(ApiFailure failure) => switch (failure) {
    ApiFailure.conflict => 'Conflito que exige revisão.',
    ApiFailure.forbidden => 'Permissão não concedida.',
    ApiFailure.unauthorized => 'Sessão inválida.',
    ApiFailure.validation ||
    ApiFailure.ruleViolation => 'Dados precisam de revisão.',
    ApiFailure.rateLimited => 'Limite temporário de sincronização.',
    _ => 'Sincronização pendente.',
  };
}
