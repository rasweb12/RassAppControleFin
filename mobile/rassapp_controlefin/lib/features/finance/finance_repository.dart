import 'dart:math';

import '../../core/network/api_client.dart';
import '../../core/storage/offline_finance_store.dart';
import '../auth/auth_controller.dart';
import '../synchronization/finance_sync_service.dart';

enum FinanceWriteState { applied, pending }

class TenantSummary {
  const TenantSummary({
    required this.id,
    required this.name,
    required this.role,
  });
  final String id;
  final String name;
  final String role;

  factory TenantSummary.fromJson(Map<String, dynamic> json) => TenantSummary(
    id: json['id'] as String,
    name: json['name'] as String,
    role: json['role'] as String,
  );
}

class ProfileSummary {
  const ProfileSummary({
    required this.id,
    required this.displayName,
    required this.version,
  });

  final String id;
  final String displayName;
  final int version;

  factory ProfileSummary.fromJson(Map<String, dynamic> json) => ProfileSummary(
    id: json['id'] as String,
    displayName: json['displayName'] as String,
    version: json['version'] as int,
  );
}

class DashboardMetric {
  const DashboardMetric({
    required this.currency,
    required this.netWorth,
    required this.income,
    required this.expense,
    required this.savingsRate,
    required this.budgetUsed,
    required this.activeGoals,
  });
  final String currency;
  final num netWorth;
  final num income;
  final num expense;
  final num savingsRate;
  final num budgetUsed;
  final int activeGoals;

  factory DashboardMetric.fromJson(Map<String, dynamic> json) =>
      DashboardMetric(
        currency: json['currency'] as String,
        netWorth: json['netWorth'] as num,
        income: json['income'] as num,
        expense: json['expense'] as num,
        savingsRate: json['savingsRate'] as num,
        budgetUsed: json['budgetUsed'] as num,
        activeGoals: json['activeGoals'] as int,
      );
}

class AccountSummary {
  const AccountSummary({
    required this.id,
    required this.name,
    required this.currency,
    required this.balance,
    required this.version,
    required this.archived,
    this.overdraftLimit = 0,
  });
  final String id;
  final String name;
  final String currency;
  final num balance;
  final int version;
  final bool archived;
  final num overdraftLimit;

  factory AccountSummary.fromJson(Map<String, dynamic> json) => AccountSummary(
    id: json['id'] as String,
    name: json['name'] as String,
    currency: json['currency'] as String,
    balance: json['balance'] as num,
    version: json['version'] as int,
    archived: json['isArchived'] as bool,
    overdraftLimit: json['overdraftLimit'] as num? ?? 0,
  );
}

class CategorySummary {
  const CategorySummary({
    required this.id,
    required this.name,
    required this.flow,
    required this.version,
    required this.archived,
  });
  final String id;
  final String name;
  final String flow;
  final int version;
  final bool archived;

  factory CategorySummary.fromJson(Map<String, dynamic> json) =>
      CategorySummary(
        id: json['id'] as String,
        name: json['name'] as String,
        flow: json['flow'] as String,
        version: json['version'] as int? ?? 0,
        archived: json['isArchived'] as bool,
      );
}

class GoalSummary {
  const GoalSummary({
    required this.id,
    required this.name,
    required this.targetAmount,
    required this.currentAmount,
    required this.currency,
    required this.targetDate,
    required this.status,
    required this.version,
  });
  final String id;
  final String name;
  final num targetAmount;
  final num currentAmount;
  final String currency;
  final DateTime? targetDate;
  final String status;
  final int version;

  double get progress => targetAmount == 0
      ? 0
      : (currentAmount / targetAmount).clamp(0, 1).toDouble();

  factory GoalSummary.fromJson(Map<String, dynamic> json) => GoalSummary(
    id: json['id'] as String,
    name: json['name'] as String,
    targetAmount: json['targetAmount'] as num,
    currentAmount: json['currentAmount'] as num,
    currency: json['currency'] as String,
    targetDate: json['targetDate'] == null
        ? null
        : DateTime.parse(json['targetDate'] as String).toLocal(),
    status: json['status'] as String,
    version: json['version'] as int,
  );
}

class ExpenseReductionGoalSummary {
  const ExpenseReductionGoalSummary({
    required this.id,
    required this.categoryId,
    required this.referenceAmount,
    required this.targetAmount,
    required this.actualExpense,
    required this.currency,
    required this.periodStart,
    required this.periodEnd,
    required this.version,
    required this.archived,
  });
  final String id;
  final String? categoryId;
  final num referenceAmount;
  final num targetAmount;
  final num actualExpense;
  final String currency;
  final DateTime periodStart;
  final DateTime periodEnd;
  final int version;
  final bool archived;

  num get requiredReduction => referenceAmount - targetAmount;
  num get achievedReduction =>
      (referenceAmount - actualExpense).clamp(0, requiredReduction);
  double get progress => requiredReduction == 0
      ? 0
      : (achievedReduction / requiredReduction).clamp(0, 1).toDouble();

  factory ExpenseReductionGoalSummary.fromJson(Map<String, dynamic> json) =>
      ExpenseReductionGoalSummary(
        id: json['id'] as String,
        categoryId: json['categoryId'] as String?,
        referenceAmount: json['referenceAmount'] as num,
        targetAmount: json['targetAmount'] as num,
        actualExpense: json['actualExpense'] as num,
        currency: json['currency'] as String,
        periodStart: DateTime.parse(json['periodStart'] as String).toLocal(),
        periodEnd: DateTime.parse(json['periodEnd'] as String).toLocal(),
        version: json['version'] as int,
        archived: json['isArchived'] as bool,
      );
}

class FinancialNotificationSummary {
  const FinancialNotificationSummary({
    required this.id,
    required this.type,
    required this.title,
    required this.message,
    required this.createdAt,
    required this.readAt,
    required this.version,
  });
  final String id;
  final String type;
  final String title;
  final String message;
  final DateTime createdAt;
  final DateTime? readAt;
  final int version;
  bool get isRead => readAt != null;
  factory FinancialNotificationSummary.fromJson(Map<String, dynamic> json) =>
      FinancialNotificationSummary(
        id: json['id'] as String,
        type: json['type'] as String,
        title: json['title'] as String,
        message: json['message'] as String,
        createdAt: DateTime.parse(json['createdAt'] as String).toLocal(),
        readAt: json['readAt'] == null
            ? null
            : DateTime.parse(json['readAt'] as String).toLocal(),
        version: json['version'] as int,
      );
}

class TransactionSummary {
  const TransactionSummary({
    required this.id,
    required this.accountId,
    required this.categoryId,
    required this.amount,
    required this.currency,
    required this.flow,
    required this.status,
    required this.occurredAt,
    required this.description,
    required this.version,
  });
  final String id;
  final String accountId;
  final String categoryId;
  final num amount;
  final String currency;
  final String flow;
  final String status;
  final DateTime occurredAt;
  final String description;
  final int version;

  factory TransactionSummary.fromJson(Map<String, dynamic> json) =>
      TransactionSummary(
        id: json['id'] as String,
        accountId: json['accountId'] as String,
        categoryId: json['categoryId'] as String,
        amount: json['amount'] as num,
        currency: json['currency'] as String,
        flow: json['flow'] as String,
        status: json['status'] as String,
        occurredAt: DateTime.parse(json['occurredAt'] as String).toLocal(),
        description: json['description'] as String,
        version: json['version'] as int,
      );
}

class FinanceRepository {
  FinanceRepository(this._auth, {Future<OfflineFinanceStore>? store})
    : _store = store ?? OfflineFinanceStore.open(_auth.session.subject);
  final AuthController _auth;
  final Future<OfflineFinanceStore> _store;

  late final Future<FinanceSyncService> _syncService = _store.then(
    (store) => FinanceSyncService(_auth, store),
  );

  Future<List<TenantSummary>> tenants() async {
    try {
      final data = await _auth.getAuthenticated('/api/v1/tenants');
      if (data is! List) {
        throw const ApiException(
          ApiFailure.unexpected,
          'Não foi possível carregar os espaços financeiros.',
        );
      }
      final resources = data.cast<Map<String, dynamic>>();
      await (await _store).cacheAll('_identity', 'tenant', resources);
      return resources.map(TenantSummary.fromJson).toList(growable: false);
    } on ApiException catch (error) {
      if (!_canReadOffline(error)) rethrow;
      final cached = await (await _store).cached('_identity', 'tenant');
      if (cached.isEmpty) rethrow;
      return cached.map(TenantSummary.fromJson).toList(growable: false);
    }
  }

  Future<List<DashboardMetric>> dashboard(String tenantId, DateTime now) async {
    final from = DateTime(now.year, now.month);
    final to = DateTime(now.year, now.month + 1);
    try {
      final data = await _auth.getAuthenticated(
        '/api/v1/dashboard',
        tenantId: tenantId,
        queryParameters: {
          'from': from.toUtc().toIso8601String(),
          'to': to.toUtc().toIso8601String(),
        },
      );
      final metrics = (data as Map<String, dynamic>)['metrics'] as List;
      final resources = metrics
          .cast<Map<String, dynamic>>()
          .map((item) => {...item, 'id': item['currency']})
          .toList(growable: false);
      await (await _store).cacheAll(tenantId, 'dashboard', resources);
      return resources.map(DashboardMetric.fromJson).toList(growable: false);
    } on ApiException catch (error) {
      if (!_canReadOffline(error)) rethrow;
      return (await (await _store).cached(
        tenantId,
        'dashboard',
      )).map(DashboardMetric.fromJson).toList(growable: false);
    }
  }

  Future<ProfileSummary> profile(String tenantId) async {
    final data = await _auth.getAuthenticated(
      '/api/v1/profile',
      tenantId: tenantId,
    );
    if (data is! Map<String, dynamic>) {
      throw const ApiException(
        ApiFailure.unexpected,
        'Não foi possível carregar seu perfil.',
      );
    }
    return ProfileSummary.fromJson(data);
  }

  Future<List<AccountSummary>> accounts(String tenantId) async {
    return _loadPage(
      tenantId: tenantId,
      endpoint: '/api/v1/accounts',
      entityType: 'account',
      parser: AccountSummary.fromJson,
    );
  }

  Future<List<TransactionSummary>> transactions(String tenantId) async {
    try {
      final maps = await _loadAllTransactionPages(tenantId);
      final store = await _store;
      await store.cacheAll(tenantId, 'transaction', maps);
      return (await store.cached(
        tenantId,
        'transaction',
      )).map(TransactionSummary.fromJson).toList(growable: false);
    } on ApiException catch (error) {
      if (!_canReadOffline(error)) rethrow;
      return (await (await _store).cached(
        tenantId,
        'transaction',
      )).map(TransactionSummary.fromJson).toList(growable: false);
    }
  }

  Future<String> askFinancialAssistant({
    required String tenantId,
    required String question,
    required String period,
  }) async {
    final data = await _auth.postAuthenticated('/api/v1/ai/ask', {
      'question': question,
      'period': period,
    }, tenantId: tenantId);
    if (data is! Map<String, dynamic> || data['text'] is! String) {
      throw const ApiException(
        ApiFailure.unexpected,
        'O assistente retornou uma resposta inválida.',
      );
    }
    return data['text'] as String;
  }

  Future<List<CategorySummary>> categories(String tenantId) async {
    return _loadPage(
      tenantId: tenantId,
      endpoint: '/api/v1/categories',
      entityType: 'category',
      parser: CategorySummary.fromJson,
      pageSize: '100',
    );
  }

  Future<List<GoalSummary>> goals(String tenantId) => _loadPage(
    tenantId: tenantId,
    endpoint: '/api/v1/goals',
    entityType: 'goal',
    parser: GoalSummary.fromJson,
    pageSize: '100',
  );

  Future<List<ExpenseReductionGoalSummary>> expenseReductionGoals(
    String tenantId,
  ) => _loadPage(
    tenantId: tenantId,
    endpoint: '/api/v1/expense-reduction-goals',
    entityType: 'expenseReductionGoal',
    parser: ExpenseReductionGoalSummary.fromJson,
    pageSize: '100',
  );
  Future<List<FinancialNotificationSummary>> notifications(String tenantId) =>
      _loadPage(
        tenantId: tenantId,
        endpoint: '/api/v1/notifications',
        entityType: 'notification',
        parser: FinancialNotificationSummary.fromJson,
        pageSize: '100',
      );
  Future<void> markNotificationRead({
    required String tenantId,
    required FinancialNotificationSummary notification,
  }) async {
    if (notification.isRead) return;
    await _auth.postAuthenticated(
      '/api/v1/notifications/${notification.id}/read',
      {'operationId': _newOperationId(), 'version': notification.version},
      tenantId: tenantId,
    );
  }

  Future<FinanceWriteState> createExpenseReductionGoal({
    required String tenantId,
    String? categoryId,
    required num referenceAmount,
    required num targetAmount,
    required String currency,
    required DateTime periodStart,
    required DateTime periodEnd,
  }) {
    final operationId = _newOperationId();
    return _postOrQueue(
      tenantId: tenantId,
      endpoint: '/api/v1/expense-reduction-goals',
      entityType: 'expenseReductionGoal',
      entityId: 'local-$operationId',
      payload: {
        'operationId': operationId,
        'categoryId': categoryId,
        'referenceAmount': referenceAmount,
        'targetAmount': targetAmount,
        'currency': currency,
        'periodStart': periodStart.toUtc().toIso8601String(),
        'periodEnd': periodEnd.toUtc().toIso8601String(),
      },
      localResource: {
        'id': 'local-$operationId',
        'categoryId': categoryId,
        'referenceAmount': referenceAmount,
        'targetAmount': targetAmount,
        'actualExpense': 0,
        'currency': currency,
        'periodStart': periodStart.toUtc().toIso8601String(),
        'periodEnd': periodEnd.toUtc().toIso8601String(),
        'version': 0,
        'isArchived': false,
      },
    );
  }

  Future<FinanceWriteState> createAccount({
    required String tenantId,
    required String name,
    required String currency,
    required num openingBalance,
    num overdraftLimit = 0,
  }) async {
    final operationId = _newOperationId();
    final result = await _postOrQueue(
      tenantId: tenantId,
      endpoint: '/api/v1/accounts',
      entityType: 'account',
      entityId: 'local-$operationId',
      payload: {
        'operationId': operationId,
        'name': name,
        'currency': currency,
        'openingBalance': openingBalance,
        'overdraftLimit': overdraftLimit,
      },
      localResource: {
        'id': 'local-$operationId',
        'name': name,
        'currency': currency,
        'balance': openingBalance,
        'overdraftLimit': overdraftLimit,
        'version': 0,
        'isArchived': false,
      },
    );
    if (result == FinanceWriteState.pending) {
      await _applyPendingDashboard(
        tenantId,
        currency,
        netWorthDelta: openingBalance,
      );
    }
    return result;
  }

  Future<FinanceWriteState> createCategory({
    required String tenantId,
    required String name,
    required String flow,
  }) {
    final operationId = _newOperationId();
    return _postOrQueue(
      tenantId: tenantId,
      endpoint: '/api/v1/categories',
      entityType: 'category',
      entityId: 'local-$operationId',
      payload: {'operationId': operationId, 'name': name, 'flow': flow},
      localResource: {
        'id': 'local-$operationId',
        'name': name,
        'flow': flow,
        'isArchived': false,
      },
    );
  }

  Future<void> maintainAccount({
    required String tenantId,
    required AccountSummary account,
    required String name,
    bool archive = false,
  }) => _auth.patchAuthenticated('/api/v1/accounts/${account.id}', {
    'operationId': _newOperationId(),
    'version': account.version,
    'name': name,
    'archive': archive,
  }, tenantId: tenantId);

  Future<void> maintainCategory({
    required String tenantId,
    required CategorySummary category,
    required String name,
    bool archive = false,
  }) => _auth.patchAuthenticated('/api/v1/categories/${category.id}', {
    'operationId': _newOperationId(),
    'version': category.version,
    'name': name,
    'archive': archive,
  }, tenantId: tenantId);

  Future<FinanceWriteState> createGoal({
    required String tenantId,
    required String name,
    required num targetAmount,
    required String currency,
    DateTime? targetDate,
  }) {
    final operationId = _newOperationId();
    return _postOrQueue(
      tenantId: tenantId,
      endpoint: '/api/v1/goals',
      entityType: 'goal',
      entityId: 'local-$operationId',
      payload: {
        'operationId': operationId,
        'name': name,
        'targetAmount': targetAmount,
        'currency': currency,
        'targetDate': targetDate?.toUtc().toIso8601String(),
      },
      localResource: {
        'id': 'local-$operationId',
        'name': name,
        'targetAmount': targetAmount,
        'currentAmount': 0,
        'currency': currency,
        'targetDate': targetDate?.toUtc().toIso8601String(),
        'status': 'Active',
        'version': 0,
      },
    );
  }

  Future<FinanceWriteState> addGoalProgress({
    required String tenantId,
    required GoalSummary goal,
    required num amount,
  }) async {
    final operationId = _newOperationId();
    final result = await _postOrQueue(
      tenantId: tenantId,
      endpoint: '/api/v1/goals/${goal.id}/progress',
      entityType: 'goal',
      entityId: goal.id,
      payload: {
        'operationId': operationId,
        'version': goal.version,
        'amount': amount,
        'currency': goal.currency,
      },
    );
    if (result == FinanceWriteState.pending) {
      final store = await _store;
      await store.cacheOne(tenantId, 'goal', {
        'id': goal.id,
        'name': goal.name,
        'targetAmount': goal.targetAmount,
        'currentAmount': (goal.currentAmount + amount).clamp(
          0,
          goal.targetAmount,
        ),
        'currency': goal.currency,
        'targetDate': goal.targetDate?.toUtc().toIso8601String(),
        'status': goal.currentAmount + amount >= goal.targetAmount
            ? 'Completed'
            : goal.status,
        'version': goal.version + 1,
      });
    }
    return result;
  }

  Future<FinanceWriteState> createTransaction({
    required String tenantId,
    required AccountSummary account,
    required CategorySummary category,
    required num amount,
    required String description,
  }) async {
    final operationId = _newOperationId();
    final occurredAt = DateTime.now().toUtc().toIso8601String();
    final result = await _postOrQueue(
      tenantId: tenantId,
      endpoint: '/api/v1/transactions',
      entityType: 'transaction',
      entityId: 'local-$operationId',
      payload: {
        'operationId': operationId,
        'accountId': account.id,
        'categoryId': category.id,
        'amount': amount,
        'currency': account.currency,
        'flow': category.flow,
        'status': 'Posted',
        'occurredAt': occurredAt,
        'description': description,
        'accountVersion': account.version,
      },
      localResource: {
        'id': 'local-$operationId',
        'amount': amount,
        'currency': account.currency,
        'flow': category.flow,
        'status': 'Pending sync',
        'occurredAt': occurredAt,
        'description': description,
      },
    );
    if (result == FinanceWriteState.pending) {
      final income = category.flow == 'Income' ? amount : 0;
      final expense = category.flow == 'Expense' ? amount : 0;
      await _adjustCachedAccount(
        tenantId,
        account,
        category.flow == 'Income' ? amount : -amount,
      );
      await _applyPendingDashboard(
        tenantId,
        account.currency,
        netWorthDelta: income - expense,
        incomeDelta: income,
        expenseDelta: expense,
      );
    }
    return result;
  }

  Future<FinanceWriteState> createTransfer({
    required String tenantId,
    required AccountSummary source,
    required AccountSummary destination,
    required num amount,
    required String description,
  }) async {
    final operationId = _newOperationId();
    final result = await _postOrQueue(
      tenantId: tenantId,
      endpoint: '/api/v1/transfers',
      entityType: 'transfer',
      entityId: 'local-$operationId',
      payload: {
        'operationId': operationId,
        'sourceAccountId': source.id,
        'destinationAccountId': destination.id,
        'amount': amount,
        'currency': source.currency,
        'occurredAt': DateTime.now().toUtc().toIso8601String(),
        'description': description,
        'sourceVersion': source.version,
        'destinationVersion': destination.version,
      },
    );
    if (result == FinanceWriteState.pending) {
      await _adjustCachedAccount(tenantId, source, -amount);
      await _adjustCachedAccount(tenantId, destination, amount);
    }
    return result;
  }

  Future<void> cancelTransaction({
    required String tenantId,
    required TransactionSummary transaction,
    required AccountSummary account,
  }) =>
      _auth.postAuthenticated('/api/v1/transactions/${transaction.id}/cancel', {
        'operationId': _newOperationId(),
        'version': transaction.version,
        'accountVersion': account.version,
      }, tenantId: tenantId);

  Future<void> editTransaction({
    required String tenantId,
    required TransactionSummary transaction,
    required AccountSummary account,
    required num amount,
    required String description,
  }) => _auth.putAuthenticated('/api/v1/transactions/${transaction.id}', {
    'operationId': _newOperationId(),
    'version': transaction.version,
    'categoryId': transaction.categoryId,
    'amount': amount,
    'currency': transaction.currency,
    'status': transaction.status,
    'occurredAt': transaction.occurredAt.toUtc().toIso8601String(),
    'description': description,
    'accountVersion': account.version,
  }, tenantId: tenantId);

  Future<FinanceSyncResult> synchronize(String tenantId) async =>
      (await _syncService).synchronize(tenantId);

  Future<int> pendingSyncCount(String tenantId) async =>
      (await _store).pendingCount(tenantId);

  Future<List<SyncConflict>> syncConflicts(String tenantId) async =>
      (await _store).conflicts(tenantId);

  Future<void> discardSyncConflict(String tenantId, String operationId) async =>
      (await _store).discardConflict(tenantId, operationId);

  Future<void> dispose() async => (await _store).close();

  Future<List<T>> _loadPage<T>({
    required String tenantId,
    required String endpoint,
    required String entityType,
    required T Function(Map<String, dynamic>) parser,
    String pageSize = '50',
  }) async {
    try {
      final data = await _auth.getAuthenticated(
        endpoint,
        tenantId: tenantId,
        queryParameters: {'page': '1', 'pageSize': pageSize},
      );

      final resources = ((data as Map<String, dynamic>)['items'] as List)
          .cast<Map<String, dynamic>>();
      await (await _store).cacheAll(tenantId, entityType, resources);
      return resources.map(parser).toList(growable: false);
    } on ApiException catch (error) {
      if (!_canReadOffline(error)) rethrow;
      return (await (await _store).cached(
        tenantId,
        entityType,
      )).map(parser).toList(growable: false);
    }
  }

  Future<List<Map<String, dynamic>>> _loadAllTransactionPages(
    String tenantId,
  ) async {
    const pageSize = 100;
    final resources = <Map<String, dynamic>>[];
    for (var pageNumber = 1; pageNumber <= 1000; pageNumber++) {
      final data = await _auth.getAuthenticated(
        '/api/v1/transactions',
        tenantId: tenantId,
        queryParameters: {'page': '$pageNumber', 'pageSize': '$pageSize'},
      );
      if (data is! Map<String, dynamic> || data['page'] is! Map) {
        throw const ApiException(
          ApiFailure.unexpected,
          'Não foi possível carregar as transações.',
        );
      }
      final page = Map<String, dynamic>.from(data['page'] as Map);
      final items = page['items'];
      final total = page['total'];
      if (items is! List || total is! num || total < 0) {
        throw const ApiException(
          ApiFailure.unexpected,
          'Não foi possível carregar as transações.',
        );
      }
      resources.addAll(
        items.map((item) => Map<String, dynamic>.from(item as Map)),
      );
      if (resources.length >= total) return resources;
      if (items.isEmpty) break;
    }
    throw const ApiException(
      ApiFailure.unexpected,
      'A paginação de transações é inválida.',
    );
  }

  Future<FinanceWriteState> _postOrQueue({
    required String tenantId,
    required String endpoint,
    required String entityType,
    required String entityId,
    required Map<String, dynamic> payload,
    Map<String, dynamic>? localResource,
  }) async {
    try {
      await _auth.postAuthenticated(endpoint, payload, tenantId: tenantId);
      return FinanceWriteState.applied;
    } on ApiException catch (error) {
      if (!_canQueueOffline(error)) rethrow;
      final store = await _store;
      await store.enqueue(
        tenantId: tenantId,
        endpoint: endpoint,
        payload: payload,
        entityType: entityType,
        entityId: entityId,
        operationType: 'create',
      );
      if (localResource != null) {
        await store.cacheOne(tenantId, entityType, localResource);
      }
      return FinanceWriteState.pending;
    }
  }

  bool _canReadOffline(ApiException error) =>
      error.failure == ApiFailure.offline ||
      error.failure == ApiFailure.timeout ||
      error.failure == ApiFailure.unavailable;

  bool _canQueueOffline(ApiException error) => _canReadOffline(error);

  Future<void> _adjustCachedAccount(
    String tenantId,
    AccountSummary account,
    num delta,
  ) async {
    final store = await _store;
    final resources = await store.cached(tenantId, 'account');
    final index = resources.indexWhere(
      (resource) => resource['id'] == account.id,
    );
    if (index < 0) return;
    final resource = {...resources[index]};
    resource['balance'] = (resource['balance'] as num) + delta;
    resource['version'] = (resource['version'] as num) + 1;
    await store.cacheOne(tenantId, 'account', resource);
  }

  Future<void> _applyPendingDashboard(
    String tenantId,
    String currency, {
    num netWorthDelta = 0,
    num incomeDelta = 0,
    num expenseDelta = 0,
  }) async {
    final store = await _store;
    final resources = await store.cached(tenantId, 'dashboard');
    final index = resources.indexWhere(
      (resource) => resource['currency'] == currency,
    );
    final resource = index < 0
        ? <String, dynamic>{
            'id': currency,
            'currency': currency,
            'netWorth': 0,
            'income': 0,
            'expense': 0,
            'savingsRate': 0,
            'budgetUsed': 0,
            'activeGoals': 0,
          }
        : {...resources[index]};
    resource['netWorth'] = (resource['netWorth'] as num) + netWorthDelta;
    resource['income'] = (resource['income'] as num) + incomeDelta;
    resource['expense'] = (resource['expense'] as num) + expenseDelta;
    final income = resource['income'] as num;
    final expense = resource['expense'] as num;
    resource['savingsRate'] = income == 0 ? 0 : (income - expense) / income;
    await store.cacheOne(tenantId, 'dashboard', resource);
  }
}

String _newOperationId() {
  final random = Random.secure();
  final bytes = List<int>.generate(16, (_) => random.nextInt(256));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  final hex = bytes
      .map((value) => value.toRadixString(16).padLeft(2, '0'))
      .join();
  return '${hex.substring(0, 8)}-${hex.substring(8, 12)}-${hex.substring(12, 16)}-${hex.substring(16, 20)}-${hex.substring(20)}';
}
