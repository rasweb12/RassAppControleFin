import 'dart:async';

import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter/material.dart';

import '../../core/network/api_client.dart';
import '../auth/auth_controller.dart';
import '../finance/finance_repository.dart';
import 'finance_forms.dart';
import '../synchronization/finance_sync_service.dart';

class FinanceHomeShell extends StatefulWidget {
  const FinanceHomeShell({required this.authController, super.key});
  final AuthController authController;

  @override
  State<FinanceHomeShell> createState() => _FinanceHomeShellState();
}

class _FinanceHomeShellState extends State<FinanceHomeShell> {
  late final FinanceRepository _repository;
  int _tab = 0;
  bool _loading = true;
  bool _sessionExpired = false;
  String? _error;
  List<TenantSummary> _tenants = const [];
  TenantSummary? _tenant;
  List<DashboardMetric> _metrics = const [];
  List<AccountSummary> _accounts = const [];
  List<CategorySummary> _categories = const [];
  List<TransactionSummary> _transactions = const [];
  StreamSubscription<List<ConnectivityResult>>? _connectivitySubscription;

  @override
  void initState() {
    super.initState();
    _repository = FinanceRepository(widget.authController);
    _connectivitySubscription = Connectivity().onConnectivityChanged.listen((
      state,
    ) {
      if (!state.contains(ConnectivityResult.none) && _tenant != null) {
        _synchronizeAndLoad();
      }
    });
    _load();
  }

  @override
  void dispose() {
    _connectivitySubscription?.cancel();
    _repository.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
      _sessionExpired = false;
    });
    try {
      final tenants = await _repository.tenants();
      if (tenants.isEmpty) {
        if (!mounted) return;
        setState(() {
          _tenants = tenants;
          _tenant = null;
          _metrics = const [];
          _accounts = const [];
          _categories = const [];
          _transactions = const [];
          _loading = false;
        });
        return;
      }
      final tenant =
          _tenant != null && tenants.any((item) => item.id == _tenant!.id)
          ? tenants.firstWhere((item) => item.id == _tenant!.id)
          : tenants.first;
      final values = await Future.wait([
        _repository.dashboard(tenant.id, DateTime.now()),
        _repository.accounts(tenant.id),
        _repository.categories(tenant.id),
        _repository.transactions(tenant.id),
      ]);
      if (!mounted) return;
      setState(() {
        _tenants = tenants;
        _tenant = tenant;
        _metrics = values[0] as List<DashboardMetric>;
        _accounts = values[1] as List<AccountSummary>;
        _categories = values[2] as List<CategorySummary>;
        _transactions = values[3] as List<TransactionSummary>;
        _loading = false;
      });
    } on ApiException catch (error) {
      if (!mounted) return;
      setState(() {
        _sessionExpired = error.failure == ApiFailure.unauthorized;
        _error = switch (error.failure) {
          ApiFailure.unauthorized => 'Sua sessão expirou. Entre novamente.',
          ApiFailure.forbidden =>
            'Você não possui acesso a este espaço financeiro.',
          ApiFailure.unavailable =>
            'Não foi possível carregar seus dados agora.',
          _ => error.message,
        };
        _loading = false;
      });
    }
  }

  Future<void> _returnToLogin() => widget.authController.signOut();

  Future<void> _chooseTenant() async {
    final selected = await showModalBottomSheet<TenantSummary>(
      context: context,
      builder: (context) => SafeArea(
        child: ListView(
          shrinkWrap: true,
          children: [
            const ListTile(title: Text('Selecionar espaço financeiro')),
            for (final tenant in _tenants)
              ListTile(
                leading: Icon(
                  tenant.id == _tenant?.id
                      ? Icons.check_circle
                      : Icons.circle_outlined,
                ),
                title: Text(tenant.name),
                subtitle: Text(tenant.role),
                onTap: () => Navigator.pop(context, tenant),
              ),
          ],
        ),
      ),
    );
    if (selected == null || selected.id == _tenant?.id) return;
    setState(() => _tenant = selected);
    await _load();
  }

  Future<void> _openAction() async {
    final action = await showFinanceActionSheet(context);
    if (action == null || !mounted) return;
    switch (action) {
      case FinanceAction.account:
        await _createAccount();
        return;
      case FinanceAction.category:
        await _createCategory();
        return;
      case FinanceAction.income:
        await _createTransaction('Income');
        return;
      case FinanceAction.expense:
        await _createTransaction('Expense');
        return;
      case FinanceAction.transfer:
        await _createTransfer();
        return;
    }
  }

  Future<void> _createAccount() async {
    final tenant = _tenant;
    if (tenant == null) return;
    final input = await showAccountForm(context);
    if (input == null || !mounted) return;
    await _mutate(
      () => _repository.createAccount(
        tenantId: tenant.id,
        name: input.name,
        currency: 'BRL',
        openingBalance: input.openingBalance,
      ),
    );
  }

  Future<void> _createCategory() async {
    final tenant = _tenant;
    if (tenant == null) return;
    final input = await showCategoryForm(context);
    if (input == null || !mounted) return;
    await _mutate(
      () => _repository.createCategory(
        tenantId: tenant.id,
        name: input.name,
        flow: input.flow,
      ),
    );
  }

  Future<void> _createTransaction(String flow) async {
    final tenant = _tenant;
    if (tenant == null) return;
    final accounts = _accounts
        .where((item) => !item.archived && !item.id.startsWith('local-'))
        .toList(growable: false);
    if (accounts.isEmpty) {
      _notice('Cadastre uma conta antes de registrar um lançamento.');
      return;
    }
    final categories = _categories
        .where(
          (item) =>
              !item.archived &&
              !item.id.startsWith('local-') &&
              item.flow == flow,
        )
        .toList(growable: false);
    if (categories.isEmpty) {
      _notice(
        'Crie uma categoria de ${flow == 'Income' ? 'receita' : 'despesa'} antes de continuar.',
      );
      return;
    }
    final input = await showTransactionForm(
      context,
      flow: flow,
      accounts: accounts,
      categories: categories,
    );
    if (input == null || !mounted) return;
    await _mutate(
      () => _repository.createTransaction(
        tenantId: tenant.id,
        account: input.account,
        category: input.category,
        amount: input.amount,
        description: input.description,
      ),
    );
  }

  Future<void> _createTransfer() async {
    final tenant = _tenant;
    final accounts = _accounts
        .where((item) => !item.archived && !item.id.startsWith('local-'))
        .toList(growable: false);
    if (tenant == null) return;
    if (accounts.length < 2) {
      _notice('Cadastre ao menos duas contas ativas para transferir valores.');
      return;
    }
    final input = await showTransferForm(context, accounts);
    if (input == null || !mounted) return;
    await _mutate(
      () => _repository.createTransfer(
        tenantId: tenant.id,
        source: input.source,
        destination: input.destination,
        amount: input.amount,
        description: input.description,
      ),
    );
  }

  Future<void> _mutate(Future<FinanceWriteState> Function() operation) async {
    try {
      final result = await operation();
      if (!mounted) return;
      _notice(
        result == FinanceWriteState.pending
            ? 'Salvo no dispositivo. Será sincronizado quando houver conexão.'
            : 'Lançamento salvo.',
      );
      await _load();
    } on ApiException catch (error) {
      if (!mounted) return;
      _notice(_messageFor(error));
      await _load();
    }
  }

  Future<void> _synchronizeAndLoad() async {
    final tenant = _tenant;
    try {
      if (tenant != null) {
        final result = await _repository.synchronize(tenant.id);
        if (mounted && result != FinanceSyncResult.synchronized) {
          _notice(switch (result) {
            FinanceSyncResult.offline =>
              'Sem conexão. Seus lançamentos pendentes permanecem protegidos neste dispositivo.',
            FinanceSyncResult.blocked =>
              'Há uma operação que precisa de revisão antes de sincronizar.',
            FinanceSyncResult.failed => 'A sincronização já está em andamento.',
            FinanceSyncResult.synchronized => '',
          });
        }
      }
    } on ApiException catch (error) {
      if (mounted) _notice(_messageFor(error));
    }
    await _load();
  }

  String _messageFor(ApiException error) => switch (error.failure) {
    ApiFailure.conflict =>
      'Os dados foram alterados. Atualize e tente novamente.',
    ApiFailure.unauthorized => 'Sua sessão expirou. Entre novamente.',
    ApiFailure.forbidden => 'Você não possui permissão para esta operação.',
    _ => error.message,
  };

  void _notice(String message) => ScaffoldMessenger.of(
    context,
  ).showSnackBar(SnackBar(content: Text(message)));

  @override
  Widget build(BuildContext context) {
    const titles = ['Dashboard', 'Transações', 'Contas', 'Menu'];
    final body = _loading
        ? const Center(child: CircularProgressIndicator())
        : _error != null
        ? _MessageState(
            icon: Icons.error_outline,
            title: 'Não foi possível abrir seus dados',
            message: _error!,
            action: _sessionExpired ? 'Entrar novamente' : 'Tentar novamente',
            onAction: _sessionExpired ? _returnToLogin : _load,
          )
        : _tenant == null
        ? const _MessageState(
            icon: Icons.domain_add_outlined,
            title: 'Nenhum espaço financeiro disponível',
            message:
                'Sua conta ainda não possui acesso a um espaço financeiro.',
          )
        : switch (_tab) {
            0 => _Dashboard(metrics: _metrics, onRefresh: _load),
            1 => _Transactions(items: _transactions, onRefresh: _load),
            2 => _Accounts(items: _accounts, onRefresh: _load),
            _ => _Menu(
              tenant: _tenant!,
              canSwitch: _tenants.length > 1,
              onSwitch: _chooseTenant,
              onSignOut: widget.authController.signOut,
              onSynchronize: _synchronizeAndLoad,
            ),
          };
    return Scaffold(
      appBar: AppBar(
        title: Text(titles[_tab]),
        actions: [
          if (!_loading && _tenant != null)
            IconButton(
              onPressed: _synchronizeAndLoad,
              tooltip: 'Atualizar dados',
              icon: const Icon(Icons.refresh),
            ),
        ],
      ),
      body: body,
      bottomNavigationBar: NavigationBar(
        selectedIndex: _tab,
        onDestinationSelected: (value) => setState(() => _tab = value),
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.dashboard_outlined),
            selectedIcon: Icon(Icons.dashboard),
            label: 'Início',
          ),
          NavigationDestination(
            icon: Icon(Icons.receipt_long_outlined),
            selectedIcon: Icon(Icons.receipt_long),
            label: 'Transações',
          ),
          NavigationDestination(
            icon: Icon(Icons.account_balance_wallet_outlined),
            selectedIcon: Icon(Icons.account_balance_wallet),
            label: 'Contas',
          ),
          NavigationDestination(
            icon: Icon(Icons.menu_outlined),
            selectedIcon: Icon(Icons.menu),
            label: 'Menu',
          ),
        ],
      ),
      floatingActionButton:
          _loading || _tenant == null || (_tab != 1 && _tab != 2)
          ? null
          : FloatingActionButton(
              onPressed: _openAction,
              tooltip: 'Novo lançamento',
              child: const Icon(Icons.add),
            ),
    );
  }
}

class _Dashboard extends StatelessWidget {
  const _Dashboard({required this.metrics, required this.onRefresh});
  final List<DashboardMetric> metrics;
  final Future<void> Function() onRefresh;

  @override
  Widget build(BuildContext context) {
    if (metrics.isEmpty) {
      return _MessageState(
        icon: Icons.insights_outlined,
        title: 'Sem movimentações neste período',
        message: 'Registre receitas ou despesas para visualizar seu resumo.',
        action: 'Atualizar',
        onAction: onRefresh,
      );
    }
    final metric = metrics.first;
    return RefreshIndicator(
      onRefresh: onRefresh,
      child: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          Text(
            'Resumo financeiro',
            style: Theme.of(
              context,
            ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 14),
          _Metric(
            title: 'Patrimônio líquido',
            value: _money(metric.netWorth, metric.currency),
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: _Metric(
                  title: 'Receitas',
                  value: _money(metric.income, metric.currency),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: _Metric(
                  title: 'Despesas',
                  value: _money(metric.expense, metric.currency),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          _Metric(
            title: 'Taxa de economia',
            value:
                '${(metric.savingsRate * 100).toStringAsFixed(1).replaceAll('.', ',')}%',
          ),
        ],
      ),
    );
  }
}

class _Transactions extends StatelessWidget {
  const _Transactions({required this.items, required this.onRefresh});
  final List<TransactionSummary> items;
  final Future<void> Function() onRefresh;

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) {
      return _MessageState(
        icon: Icons.receipt_long_outlined,
        title: 'Nenhuma transação encontrada',
        message: 'As transações lançadas aparecerão aqui.',
        action: 'Atualizar',
        onAction: onRefresh,
      );
    }
    return RefreshIndicator(
      onRefresh: onRefresh,
      child: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: items.length,
        separatorBuilder: (_, _) => const Divider(height: 1),
        itemBuilder: (context, index) {
          final item = items[index];
          final income = item.flow.toLowerCase() == 'income';
          return ListTile(
            leading: CircleAvatar(
              child: Icon(income ? Icons.arrow_downward : Icons.arrow_upward),
            ),
            title: Text(
              item.description.isEmpty ? 'Transação' : item.description,
            ),
            subtitle: Text('${_date(item.occurredAt)} · ${item.status}'),
            trailing: Text(
              '${income ? '+' : '-'}${_money(item.amount, item.currency)}',
              style: TextStyle(
                fontWeight: FontWeight.w800,
                color: income
                    ? const Color(0xFF087A36)
                    : const Color(0xFFD92D20),
              ),
            ),
          );
        },
      ),
    );
  }
}

class _Accounts extends StatelessWidget {
  const _Accounts({required this.items, required this.onRefresh});
  final List<AccountSummary> items;
  final Future<void> Function() onRefresh;

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) {
      return _MessageState(
        icon: Icons.account_balance_wallet_outlined,
        title: 'Você ainda não tem contas',
        message:
            'Cadastre uma conta para começar a organizar sua vida financeira.',
        action: 'Atualizar',
        onAction: onRefresh,
      );
    }
    return RefreshIndicator(
      onRefresh: onRefresh,
      child: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: items.length,
        separatorBuilder: (_, _) => const SizedBox(height: 10),
        itemBuilder: (context, index) {
          final item = items[index];
          return Card(
            child: ListTile(
              leading: const Icon(Icons.account_balance_outlined),
              title: Text(item.name),
              subtitle: Text(item.archived ? 'Conta arquivada' : 'Conta ativa'),
              trailing: Text(
                _money(item.balance, item.currency),
                style: Theme.of(
                  context,
                ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800),
              ),
            ),
          );
        },
      ),
    );
  }
}

class _Menu extends StatelessWidget {
  const _Menu({
    required this.tenant,
    required this.canSwitch,
    required this.onSwitch,
    required this.onSignOut,
    required this.onSynchronize,
  });
  final TenantSummary tenant;
  final bool canSwitch;
  final VoidCallback onSwitch;
  final Future<void> Function() onSignOut;
  final Future<void> Function() onSynchronize;

  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(16),
    children: [
      ListTile(
        leading: const Icon(Icons.domain_outlined),
        title: Text(tenant.name),
        subtitle: Text('Perfil: ${tenant.role}'),
        trailing: canSwitch ? const Icon(Icons.chevron_right) : null,
        onTap: canSwitch ? onSwitch : null,
      ),
      const Divider(),
      ListTile(
        leading: const Icon(Icons.sync_outlined),
        title: const Text('Sincronização'),
        subtitle: const Text('Enviar lançamentos pendentes e atualizar dados.'),
        trailing: const Icon(Icons.chevron_right),
        onTap: onSynchronize,
      ),
      const ListTile(
        leading: Icon(Icons.security_outlined),
        title: Text('Segurança da conta'),
        subtitle: Text('Sua sessão é validada pelo Neon Auth.'),
      ),
      const Divider(),
      ListTile(
        leading: const Icon(Icons.logout, color: Color(0xFFD92D20)),
        title: const Text('Sair', style: TextStyle(color: Color(0xFFD92D20))),
        onTap: () async => onSignOut(),
      ),
    ],
  );
}

class _Metric extends StatelessWidget {
  const _Metric({required this.title, required this.value});
  final String title;
  final String value;
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title),
          const SizedBox(height: 8),
          Text(
            value,
            style: Theme.of(
              context,
            ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
          ),
        ],
      ),
    ),
  );
}

class _MessageState extends StatelessWidget {
  const _MessageState({
    required this.icon,
    required this.title,
    required this.message,
    this.action,
    this.onAction,
  });
  final IconData icon;
  final String title;
  final String message;
  final String? action;
  final Future<void> Function()? onAction;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 48, color: const Color(0xFF52606D)),
          const SizedBox(height: 14),
          Text(
            title,
            textAlign: TextAlign.center,
            style: Theme.of(
              context,
            ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 8),
          Text(message, textAlign: TextAlign.center),
          if (action != null) ...[
            const SizedBox(height: 18),
            OutlinedButton(
              onPressed: () => onAction?.call(),
              child: Text(action!),
            ),
          ],
        ],
      ),
    ),
  );
}

String _money(num value, String currency) {
  final prefix = currency == 'BRL' ? 'R\$ ' : '$currency ';
  return '$prefix${value.toStringAsFixed(2).replaceAll('.', ',')}';
}

String _date(DateTime value) =>
    '${value.day.toString().padLeft(2, '0')}/${value.month.toString().padLeft(2, '0')}';
