import 'dart:math';

import '../../core/network/api_client.dart';
import '../auth/auth_controller.dart';

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
  });
  final String id;
  final String name;
  final String currency;
  final num balance;
  final int version;
  final bool archived;

  factory AccountSummary.fromJson(Map<String, dynamic> json) => AccountSummary(
    id: json['id'] as String,
    name: json['name'] as String,
    currency: json['currency'] as String,
    balance: json['balance'] as num,
    version: json['version'] as int,
    archived: json['isArchived'] as bool,
  );
}

class CategorySummary {
  const CategorySummary({
    required this.id,
    required this.name,
    required this.flow,
    required this.archived,
  });
  final String id;
  final String name;
  final String flow;
  final bool archived;

  factory CategorySummary.fromJson(Map<String, dynamic> json) =>
      CategorySummary(
        id: json['id'] as String,
        name: json['name'] as String,
        flow: json['flow'] as String,
        archived: json['isArchived'] as bool,
      );
}

class TransactionSummary {
  const TransactionSummary({
    required this.id,
    required this.amount,
    required this.currency,
    required this.flow,
    required this.status,
    required this.occurredAt,
    required this.description,
  });
  final String id;
  final num amount;
  final String currency;
  final String flow;
  final String status;
  final DateTime occurredAt;
  final String description;

  factory TransactionSummary.fromJson(Map<String, dynamic> json) =>
      TransactionSummary(
        id: json['id'] as String,
        amount: json['amount'] as num,
        currency: json['currency'] as String,
        flow: json['flow'] as String,
        status: json['status'] as String,
        occurredAt: DateTime.parse(json['occurredAt'] as String).toLocal(),
        description: json['description'] as String,
      );
}

class FinanceRepository {
  FinanceRepository(this._auth);
  final AuthController _auth;

  Future<List<TenantSummary>> tenants() async {
    final data = await _auth.getAuthenticated('/api/v1/tenants');
    if (data is! List) {
      throw const ApiException(
        ApiFailure.unexpected,
        'Não foi possível carregar os espaços financeiros.',
      );
    }
    return data
        .cast<Map<String, dynamic>>()
        .map(TenantSummary.fromJson)
        .toList(growable: false);
  }

  Future<List<DashboardMetric>> dashboard(String tenantId, DateTime now) async {
    final from = DateTime(now.year, now.month);
    final to = DateTime(now.year, now.month + 1);
    final data = await _auth.getAuthenticated(
      '/api/v1/dashboard',
      tenantId: tenantId,
      queryParameters: {
        'from': from.toUtc().toIso8601String(),
        'to': to.toUtc().toIso8601String(),
      },
    );
    final map = data as Map<String, dynamic>;
    return (map['metrics'] as List)
        .cast<Map<String, dynamic>>()
        .map(DashboardMetric.fromJson)
        .toList(growable: false);
  }

  Future<List<AccountSummary>> accounts(String tenantId) async {
    final data = await _auth.getAuthenticated(
      '/api/v1/accounts',
      tenantId: tenantId,
      queryParameters: const {'page': '1', 'pageSize': '50'},
    );
    final page = data as Map<String, dynamic>;
    return (page['items'] as List)
        .cast<Map<String, dynamic>>()
        .map(AccountSummary.fromJson)
        .toList(growable: false);
  }

  Future<List<TransactionSummary>> transactions(String tenantId) async {
    final data = await _auth.getAuthenticated(
      '/api/v1/transactions',
      tenantId: tenantId,
      queryParameters: const {'page': '1', 'pageSize': '50'},
    );
    final page = (data as Map<String, dynamic>)['page'] as Map<String, dynamic>;
    return (page['items'] as List)
        .cast<Map<String, dynamic>>()
        .map(TransactionSummary.fromJson)
        .toList(growable: false);
  }

  Future<List<CategorySummary>> categories(String tenantId) async {
    final data = await _auth.getAuthenticated(
      '/api/v1/categories',
      tenantId: tenantId,
      queryParameters: const {'page': '1', 'pageSize': '100'},
    );
    final page = data as Map<String, dynamic>;
    return (page['items'] as List)
        .cast<Map<String, dynamic>>()
        .map(CategorySummary.fromJson)
        .toList(growable: false);
  }

  Future<void> createAccount({
    required String tenantId,
    required String name,
    required String currency,
    required num openingBalance,
  }) => _auth.postAuthenticated('/api/v1/accounts', {
    'operationId': _newOperationId(),
    'name': name,
    'currency': currency,
    'openingBalance': openingBalance,
  }, tenantId: tenantId);

  Future<void> createCategory({
    required String tenantId,
    required String name,
    required String flow,
  }) => _auth.postAuthenticated('/api/v1/categories', {
    'operationId': _newOperationId(),
    'name': name,
    'flow': flow,
  }, tenantId: tenantId);

  Future<void> createTransaction({
    required String tenantId,
    required AccountSummary account,
    required CategorySummary category,
    required num amount,
    required String description,
  }) => _auth.postAuthenticated('/api/v1/transactions', {
    'operationId': _newOperationId(),
    'accountId': account.id,
    'categoryId': category.id,
    'amount': amount,
    'currency': account.currency,
    'flow': category.flow,
    'status': 'Posted',
    'occurredAt': DateTime.now().toUtc().toIso8601String(),
    'description': description,
    'accountVersion': account.version,
  }, tenantId: tenantId);

  Future<void> createTransfer({
    required String tenantId,
    required AccountSummary source,
    required AccountSummary destination,
    required num amount,
    required String description,
  }) => _auth.postAuthenticated('/api/v1/transfers', {
    'operationId': _newOperationId(),
    'sourceAccountId': source.id,
    'destinationAccountId': destination.id,
    'amount': amount,
    'currency': source.currency,
    'occurredAt': DateTime.now().toUtc().toIso8601String(),
    'description': description,
    'sourceVersion': source.version,
    'destinationVersion': destination.version,
  }, tenantId: tenantId);
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
