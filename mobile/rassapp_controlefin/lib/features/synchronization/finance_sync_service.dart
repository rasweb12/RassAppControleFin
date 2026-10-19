import '../../core/network/api_client.dart';
import '../../core/storage/offline_finance_store.dart';
import '../auth/auth_controller.dart';

enum FinanceSyncResult { synchronized, offline, blocked, failed }

class FinanceSyncService {
  FinanceSyncService(this._auth, this._store);

  final AuthController _auth;
  final OfflineFinanceStore _store;
  bool _running = false;

  static const _knownEventTypes = <String>{
    'OpenAccountCommand',
    'MaintainResourceCommand',
    'CreateCategoryCommand',
    'CreateCardCommand',
    'CreateTransactionCommand',
    'EditTransactionCommand',
    'CancelTransactionCommand',
    'TransferCommand',
    'CardMovementCommand',
    'CreateBudgetCommand',
    'ArchiveBudgetCommand',
    'CreateGoalCommand',
    'AddGoalProgressCommand',
    'ArchiveGoalCommand',
    'CreateExpenseReductionGoalCommand',
    'ArchiveExpenseReductionGoalCommand',
  };

  Future<FinanceSyncResult> synchronize(String tenantId) async {
    if (_running) return FinanceSyncResult.failed;
    _running = true;
    try {
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
      try {
        await _pullChanges(tenantId);
      } on ApiException catch (error) {
        if (_isRetryable(error.failure)) return FinanceSyncResult.offline;
        rethrow;
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

  Future<void> _pullChanges(String tenantId) async {
    var cursor = await _store.cursor(tenantId);
    do {
      final data = await _auth.getAuthenticated(
        '/api/v1/sync/pull',
        tenantId: tenantId,
        queryParameters: {'limit': '100', 'cursor': ?cursor},
      );
      final page = _asMap(data, 'Resposta de sincronizacao invalida.');
      final changes = _changesFrom(page['changes']);
      final appliedCursor = _optionalText(page['appliedCursor']);
      if (changes.isNotEmpty && appliedCursor == null) {
        throw const ApiException(
          ApiFailure.unexpected,
          'Resposta de sincronizacao sem cursor aplicado.',
        );
      }

      final snapshots = changes.isEmpty
          ? const <String, List<Map<String, dynamic>>>{}
          : await _loadSnapshots(tenantId);
      await _store.applyRemotePage(
        tenantId: tenantId,
        changes: changes,
        snapshots: snapshots,
        appliedCursor: appliedCursor,
      );
      final next = _optionalText(page['nextCursor']);
      if (page['nextCursor'] != null && next == null) {
        throw const ApiException(
          ApiFailure.unexpected,
          'Resposta de sincronizacao com cursor invalido.',
        );
      }
      cursor = next;
    } while (cursor != null);
  }

  List<RemoteSyncChange> _changesFrom(Object? value) {
    if (value is! List) {
      throw const ApiException(
        ApiFailure.unexpected,
        'Resposta de sincronizacao invalida.',
      );
    }
    return value
        .map((item) {
          final change = _asMap(item, 'Alteracao de sincronizacao invalida.');
          final operationId = _requiredText(change['operationId']);
          final resourceId = _requiredText(change['resourceId']);
          final eventType = _requiredText(change['eventType']);
          if (!_knownEventTypes.contains(eventType)) {
            throw const ApiException(
              ApiFailure.unexpected,
              'Tipo de alteracao de sincronizacao nao suportado.',
            );
          }
          return RemoteSyncChange(
            operationId: operationId,
            resourceId: resourceId,
            eventType: eventType,
          );
        })
        .toList(growable: false);
  }

  Future<Map<String, List<Map<String, dynamic>>>> _loadSnapshots(
    String tenantId,
  ) async {
    final accounts = await _loadPaged('/api/v1/accounts', tenantId);
    final categories = await _loadPaged('/api/v1/categories', tenantId);
    final cards = await _loadPaged('/api/v1/credit-cards', tenantId);
    final transactions = await _loadPaged(
      '/api/v1/transactions',
      tenantId,
      nestedPage: true,
    );
    final transfers = await _loadPaged('/api/v1/transfers', tenantId);
    final budgets = await _loadPaged('/api/v1/budgets', tenantId);
    final goals = await _loadPaged('/api/v1/goals', tenantId);
    final expenseReductionGoals = await _loadPaged(
      '/api/v1/expense-reduction-goals',
      tenantId,
    );
    final movements = <Map<String, dynamic>>[];
    for (final card in cards) {
      final cardId = _requiredText(card['id']);
      movements.addAll(
        await _loadPaged('/api/v1/credit-cards/$cardId/movements', tenantId),
      );
    }

    return {
      'account': accounts,
      'category': categories,
      'card': cards,
      'transaction': transactions,
      'transfer': transfers,
      'budget': budgets,
      'goal': goals,
      'expenseReductionGoal': expenseReductionGoals,
      'cardMovement': movements,
      'dashboard': await _loadDashboard(tenantId),
    };
  }

  Future<List<Map<String, dynamic>>> _loadPaged(
    String path,
    String tenantId, {
    bool nestedPage = false,
  }) async {
    const pageSize = 100;
    final resources = <Map<String, dynamic>>[];
    var pageNumber = 1;
    while (true) {
      final data = await _auth.getAuthenticated(
        path,
        tenantId: tenantId,
        queryParameters: {'page': '$pageNumber', 'pageSize': '$pageSize'},
      );
      final root = _asMap(data, 'Pagina financeira invalida.');
      final page = nestedPage
          ? _asMap(root['page'], 'Pagina financeira invalida.')
          : root;
      final rawItems = page['items'];
      final total = page['total'];
      if (rawItems is! List || total is! num || total < 0) {
        throw const ApiException(
          ApiFailure.unexpected,
          'Pagina financeira invalida.',
        );
      }
      final items = rawItems
          .map((item) => _asMap(item, 'Item financeiro invalido.'))
          .toList(growable: false);
      resources.addAll(items);
      if (resources.length >= total) return resources;
      if (items.isEmpty || pageNumber >= 1000) {
        throw const ApiException(
          ApiFailure.unexpected,
          'Paginacao financeira inconsistente.',
        );
      }
      pageNumber++;
    }
  }

  Future<List<Map<String, dynamic>>> _loadDashboard(String tenantId) async {
    final today = DateTime.now();
    final from = DateTime(today.year, today.month);
    final to = DateTime(
      today.year,
      today.month + 1,
    ).subtract(const Duration(days: 1));
    final data = await _auth.getAuthenticated(
      '/api/v1/dashboard',
      tenantId: tenantId,
      queryParameters: {
        'from': from.toIso8601String(),
        'to': to.toIso8601String(),
      },
    );
    final dashboard = _asMap(data, 'Dashboard invalido.');
    final metrics = dashboard['metrics'];
    if (metrics is! List) {
      throw const ApiException(ApiFailure.unexpected, 'Dashboard invalido.');
    }
    return metrics
        .map((item) {
          final metric = _asMap(item, 'Metrica do dashboard invalida.');
          return {...metric, 'id': _requiredText(metric['currency'])};
        })
        .toList(growable: false);
  }

  Map<String, dynamic> _asMap(Object? value, String message) {
    if (value is Map<String, dynamic>) return value;
    if (value is Map) return Map<String, dynamic>.from(value);
    throw ApiException(ApiFailure.unexpected, message);
  }

  String _requiredText(Object? value) {
    final text = _optionalText(value);
    if (text == null) {
      throw const ApiException(
        ApiFailure.unexpected,
        'Resposta de sincronizacao invalida.',
      );
    }
    return text;
  }

  String? _optionalText(Object? value) {
    if (value is! String) return null;
    final text = value.trim();
    return text.isEmpty ? null : text;
  }
}
