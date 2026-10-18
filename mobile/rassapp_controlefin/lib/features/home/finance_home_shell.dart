import 'dart:async';

import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter/material.dart';

import '../../core/network/api_client.dart';
import '../../core/storage/offline_finance_store.dart';
import '../auth/auth_controller.dart';
import '../finance/finance_repository.dart';
import '../finance/financial_assistant_page.dart';
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
  List<GoalSummary> _goals = const [];
  List<TransactionSummary> _transactions = const [];
  List<SyncConflict> _conflicts = const [];
  String? _financialInsight;
  bool _insightLoading = false;
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
          _goals = const [];
          _transactions = const [];
          _conflicts = const [];
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
        _repository.goals(tenant.id),
        _repository.transactions(tenant.id),
        _repository.syncConflicts(tenant.id),
      ]);
      if (!mounted) return;
      setState(() {
        _tenants = tenants;
        _tenant = tenant;
        _metrics = values[0] as List<DashboardMetric>;
        _accounts = values[1] as List<AccountSummary>;
        _categories = values[2] as List<CategorySummary>;
        _goals = values[3] as List<GoalSummary>;
        _transactions = values[4] as List<TransactionSummary>;
        _conflicts = values[5] as List<SyncConflict>;
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

  Future<void> _openCategories() async {
    await Navigator.of(context).push<void>(
      MaterialPageRoute(
        builder: (_) => _CategoriesPage(
          categories: _categories,
          onCreate: _createCategory,
          onRefresh: () async {
            await _load();
            return _categories;
          },
        ),
      ),
    );
  }

  Future<void> _openGoals() async {
    await Navigator.of(context).push<void>(
      MaterialPageRoute(
        builder: (_) => _GoalsPage(
          goals: _goals,
          onCreate: _createGoal,
          onRefresh: () async {
            await _load();
            return _goals;
          },
          onAddProgress: _addGoalProgress,
        ),
      ),
    );
  }

  Future<void> _openFinancialAssistant() async {
    final tenant = _tenant;
    if (tenant == null) return;
    await Navigator.of(context).push<void>(
      MaterialPageRoute(
        builder: (_) => FinancialAssistantPage(
          repository: _repository,
          tenantId: tenant.id,
          subject: widget.authController.session.subject,
        ),
      ),
    );
  }

  Future<void> _createGoal() async {
    final tenant = _tenant;
    if (tenant == null) return;
    final input = await showGoalForm(context);
    if (input == null || !mounted) return;
    await _mutate(
      () => _repository.createGoal(
        tenantId: tenant.id,
        name: input.name,
        targetAmount: input.targetAmount,
        currency: 'BRL',
        targetDate: input.targetDate,
      ),
    );
  }

  Future<void> _addGoalProgress(GoalSummary goal) async {
    final tenant = _tenant;
    if (tenant == null || goal.id.startsWith('local-')) return;
    final controller = TextEditingController();
    final amount = await showDialog<num>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text('Adicionar à meta ${goal.name}'),
        content: TextField(
          controller: controller,
          autofocus: true,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          decoration: const InputDecoration(labelText: 'Valor (R\$)'),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () {
              final value = _parseAmount(controller.text);
              if (value == null || value <= 0) return;
              Navigator.pop(dialogContext, value);
            },
            child: const Text('Adicionar'),
          ),
        ],
      ),
    );
    controller.dispose();
    if (amount == null || !mounted) return;
    await _mutate(
      () => _repository.addGoalProgress(
        tenantId: tenant.id,
        goal: goal,
        amount: amount,
      ),
    );
  }

  Future<CategorySummary?> _createCategoryForTransaction(String flow) async {
    final tenant = _tenant;
    if (tenant == null) return null;
    final input = await showCategoryForm(
      context,
      initialFlow: flow,
      lockFlow: true,
    );
    if (input == null || !mounted) return null;
    try {
      final state = await _repository.createCategory(
        tenantId: tenant.id,
        name: input.name,
        flow: flow,
      );
      if (state == FinanceWriteState.pending) {
        _notice(
          'Categoria salva no dispositivo. Conecte-se para sincronizá-la antes de usá-la em um lançamento.',
        );
        await _load();
        return null;
      }
      await _load();
      final matches = _categories.where(
        (item) =>
            item.flow == flow &&
            item.name.toLowerCase() == input.name.toLowerCase() &&
            !item.archived &&
            !item.id.startsWith('local-'),
      );
      if (matches.isEmpty) {
        _notice(
          'A categoria foi criada, mas ainda não está disponível. Atualize e tente novamente.',
        );
        return null;
      }
      return matches.first;
    } on ApiException catch (error) {
      if (mounted) _notice(_messageFor(error));
      return null;
    }
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
    final input = await showTransactionForm(
      context,
      flow: flow,
      accounts: accounts,
      categories: categories,
      onCreateCategory: _createCategoryForTransaction,
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
            : 'Alteração salva.',
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

  Future<void> _generateInsight() async {
    final tenant = _tenant;
    if (tenant == null || _insightLoading) return;
    setState(() => _insightLoading = true);
    final now = DateTime.now();
    final period = '${now.year}-${now.month.toString().padLeft(2, '0')}';
    try {
      final insight = await _repository.askFinancialAssistant(
        tenantId: tenant.id,
        question:
            'Como está minha vida financeira neste mês? Explique os principais números e uma ação prática.',
        period: period,
      );
      if (mounted) setState(() => _financialInsight = insight);
    } on ApiException catch (error) {
      if (mounted) _notice(_messageFor(error));
    } finally {
      if (mounted) setState(() => _insightLoading = false);
    }
  }

  Future<void> _reviewConflicts() async {
    final tenant = _tenant;
    if (tenant == null || _conflicts.isEmpty) {
      _notice('Não há operações pendentes de revisão.');
      return;
    }

    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (sheetContext) => SafeArea(
        child: ListView.separated(
          shrinkWrap: true,
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
          itemCount: _conflicts.length + 1,
          separatorBuilder: (_, _) => const Divider(height: 1),
          itemBuilder: (context, index) {
            if (index == 0) {
              return const ListTile(
                leading: Icon(Icons.warning_amber_outlined),
                title: Text('Operações que precisam de revisão'),
                subtitle: Text(
                  'Atualize seus dados antes de descartar uma operação local.',
                ),
              );
            }
            final conflict = _conflicts[index - 1];
            return ListTile(
              leading: const Icon(Icons.sync_problem_outlined),
              title: Text(_conflictTitle(conflict)),
              subtitle: Text(conflict.safeError),
              trailing: const Icon(Icons.chevron_right),
              onTap: () async {
                final discard = await showDialog<bool>(
                  context: sheetContext,
                  builder: (dialogContext) => AlertDialog(
                    title: const Text('Descartar operação local?'),
                    content: const Text(
                      'Esta ação remove somente a operação pendente deste dispositivo. '
                      'Ela não altera nenhum lançamento já salvo no servidor.',
                    ),
                    actions: [
                      TextButton(
                        onPressed: () => Navigator.pop(dialogContext, false),
                        child: const Text('Cancelar'),
                      ),
                      FilledButton(
                        onPressed: () => Navigator.pop(dialogContext, true),
                        child: const Text('Descartar'),
                      ),
                    ],
                  ),
                );
                if (discard != true) return;
                await _repository.discardSyncConflict(
                  tenant.id,
                  conflict.operationId,
                );
                if (sheetContext.mounted) Navigator.pop(sheetContext);
              },
            );
          },
        ),
      ),
    );
    if (mounted) await _load();
  }

  String _conflictTitle(SyncConflict conflict) => switch (conflict.entityType) {
    'transaction' => 'Lançamento pendente',
    'transfer' => 'Transferência pendente',
    'account' => 'Conta pendente',
    'category' => 'Categoria pendente',
    _ => 'Operação pendente',
  };

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
            0 => _Dashboard(
              metrics: _metrics,
              onRefresh: _load,
              insight: _financialInsight,
              insightLoading: _insightLoading,
              onGenerateInsight: _generateInsight,
            ),
            1 => _Transactions(items: _transactions, onRefresh: _load),
            2 => _Accounts(items: _accounts, onRefresh: _load),
            _ => _Menu(
              tenant: _tenant!,
              canSwitch: _tenants.length > 1,
              onSwitch: _chooseTenant,
              onSignOut: widget.authController.signOut,
              onSynchronize: _synchronizeAndLoad,
              conflictCount: _conflicts.length,
              onReviewConflicts: _reviewConflicts,
              onCategories: _openCategories,
              onGoals: _openGoals,
              onFinancialAssistant: _openFinancialAssistant,
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
  const _Dashboard({
    required this.metrics,
    required this.onRefresh,
    required this.insight,
    required this.insightLoading,
    required this.onGenerateInsight,
  });
  final List<DashboardMetric> metrics;
  final Future<void> Function() onRefresh;
  final String? insight;
  final bool insightLoading;
  final Future<void> Function() onGenerateInsight;

  @override
  Widget build(BuildContext context) {
    final metric = metrics.isEmpty ? null : metrics.first;
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
          if (metric == null)
            const Padding(
              padding: EdgeInsets.only(bottom: 12),
              child: Text(
                'Registre receitas ou despesas para visualizar seu resumo.',
              ),
            )
          else ...[
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
          const SizedBox(height: 20),
          Text(
            'Insight financeiro',
            style: Theme.of(
              context,
            ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 8),
          if (insight != null)
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Text(insight!),
              ),
            ),
          FilledButton.icon(
            onPressed: insightLoading ? null : onGenerateInsight,
            icon: insightLoading
                ? const SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : const Icon(Icons.auto_awesome_outlined),
            label: Text(
              insightLoading ? 'Gerando insight...' : 'Gerar insight do mês',
            ),
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
            onTap: () => _showTransactionDetails(context, item),
          );
        },
      ),
    );
  }
}

Future<void> _showTransactionDetails(
  BuildContext context,
  TransactionSummary transaction,
) => showModalBottomSheet<void>(
  context: context,
  builder: (context) {
    final income = transaction.flow.toLowerCase() == 'income';
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(24, 20, 24, 28),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              transaction.description.isEmpty
                  ? 'Detalhes do lançamento'
                  : transaction.description,
              style: Theme.of(
                context,
              ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
            ),
            const SizedBox(height: 16),
            _DetailRow(
              label: income ? 'Receita' : 'Despesa',
              value:
                  '${income ? '+' : '-'}${_money(transaction.amount, transaction.currency)}',
            ),
            _DetailRow(label: 'Data', value: _date(transaction.occurredAt)),
            _DetailRow(label: 'Status', value: transaction.status),
          ],
        ),
      ),
    );
  },
);

class _DetailRow extends StatelessWidget {
  const _DetailRow({required this.label, required this.value});
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 10),
    child: Row(
      children: [
        Expanded(child: Text(label)),
        Text(value, style: const TextStyle(fontWeight: FontWeight.w700)),
      ],
    ),
  );
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
    required this.conflictCount,
    required this.onReviewConflicts,
    required this.onCategories,
    required this.onGoals,
    required this.onFinancialAssistant,
  });
  final TenantSummary tenant;
  final bool canSwitch;
  final VoidCallback onSwitch;
  final Future<void> Function() onSignOut;
  final Future<void> Function() onSynchronize;
  final int conflictCount;
  final Future<void> Function() onReviewConflicts;
  final Future<void> Function() onCategories;
  final Future<void> Function() onGoals;
  final Future<void> Function() onFinancialAssistant;

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
        leading: const Icon(Icons.category_outlined),
        title: const Text('Categorias'),
        subtitle: const Text('Organize receitas e despesas.'),
        trailing: const Icon(Icons.chevron_right),
        onTap: onCategories,
      ),
      ListTile(
        leading: const Icon(Icons.flag_outlined),
        title: const Text('Metas'),
        subtitle: const Text('Acompanhe objetivos financeiros.'),
        trailing: const Icon(Icons.chevron_right),
        onTap: onGoals,
      ),
      ListTile(
        leading: const Icon(Icons.auto_awesome_outlined),
        title: const Text('Assistente financeiro'),
        subtitle: const Text('Pergunte sobre dados autorizados.'),
        trailing: const Icon(Icons.chevron_right),
        onTap: onFinancialAssistant,
      ),
      ListTile(
        leading: const Icon(Icons.sync_outlined),
        title: const Text('Sincronização'),
        subtitle: const Text('Enviar lançamentos pendentes e atualizar dados.'),
        trailing: const Icon(Icons.chevron_right),
        onTap: onSynchronize,
      ),
      if (conflictCount > 0)
        ListTile(
          leading: const Icon(
            Icons.warning_amber_outlined,
            color: Color(0xFFD92D20),
          ),
          title: const Text('Revisar sincronização'),
          subtitle: Text(
            '$conflictCount ${conflictCount == 1 ? 'operação precisa' : 'operações precisam'} de revisão.',
          ),
          trailing: const Icon(Icons.chevron_right),
          onTap: onReviewConflicts,
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

class _CategoriesPage extends StatefulWidget {
  const _CategoriesPage({
    required this.categories,
    required this.onCreate,
    required this.onRefresh,
  });
  final List<CategorySummary> categories;
  final Future<void> Function() onCreate;
  final Future<List<CategorySummary>> Function() onRefresh;

  @override
  State<_CategoriesPage> createState() => _CategoriesPageState();
}

class _CategoriesPageState extends State<_CategoriesPage> {
  late List<CategorySummary> _categories = widget.categories;
  bool _loading = false;

  Future<void> _refresh() async {
    setState(() => _loading = true);
    final refreshed = await widget.onRefresh();
    if (mounted) {
      setState(() {
        _categories = refreshed;
        _loading = false;
      });
    }
  }

  Future<void> _create() async {
    await widget.onCreate();
    await _refresh();
  }

  @override
  Widget build(BuildContext context) {
    final active = _categories.where((item) => !item.archived).toList();
    return Scaffold(
      appBar: AppBar(title: const Text('Categorias')),
      floatingActionButton: FloatingActionButton(
        onPressed: _loading ? null : _create,
        tooltip: 'Nova categoria',
        child: const Icon(Icons.add),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : active.isEmpty
          ? _MessageState(
              icon: Icons.category_outlined,
              title: 'Nenhuma categoria cadastrada',
              message: 'Crie categorias para classificar receitas e despesas.',
              action: 'Atualizar',
              onAction: _refresh,
            )
          : RefreshIndicator(
              onRefresh: _refresh,
              child: ListView.separated(
                padding: const EdgeInsets.all(16),
                itemCount: active.length,
                separatorBuilder: (_, _) => const Divider(height: 1),
                itemBuilder: (context, index) {
                  final category = active[index];
                  final income = category.flow.toLowerCase() == 'income';
                  return ListTile(
                    leading: CircleAvatar(
                      backgroundColor: income
                          ? const Color(0xFFDDF7E8)
                          : const Color(0xFFFDE8E7),
                      child: Icon(
                        income ? Icons.arrow_downward : Icons.arrow_upward,
                        color: income
                            ? const Color(0xFF087A36)
                            : const Color(0xFFD92D20),
                      ),
                    ),
                    title: Text(category.name),
                    subtitle: Text(income ? 'Receita' : 'Despesa'),
                  );
                },
              ),
            ),
    );
  }
}

class _GoalsPage extends StatefulWidget {
  const _GoalsPage({
    required this.goals,
    required this.onCreate,
    required this.onRefresh,
    required this.onAddProgress,
  });

  final List<GoalSummary> goals;
  final Future<void> Function() onCreate;
  final Future<List<GoalSummary>> Function() onRefresh;
  final Future<void> Function(GoalSummary goal) onAddProgress;

  @override
  State<_GoalsPage> createState() => _GoalsPageState();
}

class _GoalsPageState extends State<_GoalsPage> {
  late List<GoalSummary> _goals = widget.goals;
  bool _loading = false;

  Future<void> _refresh() async {
    setState(() => _loading = true);
    try {
      final refreshed = await widget.onRefresh();
      if (mounted) setState(() => _goals = refreshed);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _create() async {
    await widget.onCreate();
    await _refresh();
  }

  Future<void> _addProgress(GoalSummary goal) async {
    await widget.onAddProgress(goal);
    await _refresh();
  }

  @override
  Widget build(BuildContext context) {
    final active = _goals.where((goal) => goal.status == 'Active').toList();
    return Scaffold(
      appBar: AppBar(title: const Text('Metas')),
      floatingActionButton: FloatingActionButton(
        onPressed: _loading ? null : _create,
        tooltip: 'Nova meta',
        child: const Icon(Icons.add),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : active.isEmpty
          ? _MessageState(
              icon: Icons.flag_outlined,
              title: 'Nenhuma meta ativa',
              message: 'Crie uma meta para acompanhar seus objetivos.',
              action: 'Atualizar',
              onAction: _refresh,
            )
          : RefreshIndicator(
              onRefresh: _refresh,
              child: ListView.separated(
                padding: const EdgeInsets.all(16),
                itemCount: active.length,
                separatorBuilder: (_, _) => const SizedBox(height: 10),
                itemBuilder: (context, index) {
                  final goal = active[index];
                  final localOnly = goal.id.startsWith('local-');
                  return Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Expanded(
                                child: Text(
                                  goal.name,
                                  style: Theme.of(context).textTheme.titleMedium
                                      ?.copyWith(fontWeight: FontWeight.w800),
                                ),
                              ),
                              if (localOnly)
                                const Icon(
                                  Icons.cloud_upload_outlined,
                                  semanticLabel: 'Aguardando sincronização',
                                ),
                            ],
                          ),
                          const SizedBox(height: 8),
                          Text(
                            '${_money(goal.currentAmount, goal.currency)} de ${_money(goal.targetAmount, goal.currency)}',
                          ),
                          const SizedBox(height: 8),
                          LinearProgressIndicator(value: goal.progress),
                          const SizedBox(height: 8),
                          Text(
                            goal.targetDate == null
                                ? 'Sem data alvo'
                                : 'Meta para ${_date(goal.targetDate!)}',
                          ),
                          const SizedBox(height: 8),
                          Align(
                            alignment: Alignment.centerRight,
                            child: TextButton.icon(
                              onPressed: localOnly
                                  ? null
                                  : () => _addProgress(goal),
                              icon: const Icon(Icons.add),
                              label: const Text('Adicionar valor'),
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                },
              ),
            ),
    );
  }
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

num? _parseAmount(String value) {
  var normalized = value.trim();
  if (normalized.contains(',') && normalized.contains('.')) {
    normalized = normalized.replaceAll('.', '').replaceAll(',', '.');
  } else {
    normalized = normalized.replaceAll(',', '.');
  }
  return num.tryParse(normalized);
}
