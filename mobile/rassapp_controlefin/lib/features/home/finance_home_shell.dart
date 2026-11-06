import 'dart:async';

import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter/material.dart';

import '../../core/network/api_client.dart';
import '../../core/storage/offline_finance_store.dart';
import '../../core/theme/app_theme.dart';
import '../auth/auth_controller.dart';
import '../finance/finance_repository.dart';
import '../finance/credit_cards_page.dart';
import '../finance/financial_assistant_page.dart';
import '../finance/financial_notifications_page.dart';
import '../finance/goal_progress_dialog.dart';
import '../finance/pay_yourself_first_page.dart';
import '../monetization/entitlements_page.dart';
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
  bool _mutating = false;
  bool _sessionExpired = false;
  String? _error;
  List<TenantSummary> _tenants = const [];
  TenantSummary? _tenant;
  ProfileSummary? _profile;
  List<DashboardMetric> _metrics = const [];
  List<AccountSummary> _accounts = const [];
  List<CategorySummary> _categories = const [];
  List<GoalSummary> _goals = const [];
  List<ExpenseReductionGoalSummary> _expenseReductionGoals = const [];
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
          _profile = null;
          _metrics = const [];
          _accounts = const [];
          _categories = const [];
          _goals = const [];
          _expenseReductionGoals = const [];
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
        _repository.profile(tenant.id),
        _repository.accounts(tenant.id),
        _repository.categories(tenant.id),
        _repository.goals(tenant.id),
        _repository.expenseReductionGoals(tenant.id),
        _repository.transactions(tenant.id),
        _repository.syncConflicts(tenant.id),
      ]);
      if (!mounted) return;
      setState(() {
        _tenants = tenants;
        _tenant = tenant;
        _profile = values[1] as ProfileSummary;
        _metrics = values[0] as List<DashboardMetric>;
        _accounts = values[2] as List<AccountSummary>;
        _categories = values[3] as List<CategorySummary>;
        _goals = values[4] as List<GoalSummary>;
        _expenseReductionGoals = values[5] as List<ExpenseReductionGoalSummary>;
        _transactions = values[6] as List<TransactionSummary>;
        _conflicts = values[7] as List<SyncConflict>;
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
        overdraftLimit: input.overdraftLimit,
        accountType: input.accountType,
        subtype: input.subtype,
        institution: input.institution,
        includeInNetWorth: input.includeInNetWorth,
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
          onMaintain: _maintainCategory,
          onRefresh: () async {
            await _load();
            return _categories;
          },
        ),
      ),
    );
  }

  Future<void> _openCreditCards() async {
    final tenant = _tenant;
    if (tenant == null) return;
    await Navigator.of(context).push<void>(
      MaterialPageRoute(
        builder: (_) => CreditCardsPage(
          repository: _repository,
          tenantId: tenant.id,
          accounts: _accounts,
          categories: _categories,
        ),
      ),
    );
    if (mounted) await _load();
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
          onEdit: _editGoal,
          onArchive: _archiveGoal,
        ),
      ),
    );
  }

  Future<void> _openExpenseReductionGoals() async {
    final tenant = _tenant;
    if (tenant == null) return;
    await Navigator.of(context).push<void>(
      MaterialPageRoute(
        builder: (_) => _ExpenseReductionGoalsPage(
          goals: _expenseReductionGoals,
          categories: _categories
              .where(
                (category) => !category.archived && category.flow == 'Expense',
              )
              .toList(growable: false),
          onRefresh: () async {
            await _load();
            return _expenseReductionGoals;
          },
          onOpportunities: (months) => _repository
              .expenseReductionOpportunities(tenant.id, months: months),
          onCreate: (categoryId, reference, target) => _mutate(
            () => _repository.createExpenseReductionGoal(
              tenantId: tenant.id,
              categoryId: categoryId,
              referenceAmount: reference,
              targetAmount: target,
              currency: 'BRL',
              periodStart: DateTime.utc(
                DateTime.now().year,
                DateTime.now().month,
              ),
              periodEnd: DateTime.utc(
                DateTime.now().year,
                DateTime.now().month + 1,
              ),
            ),
          ),
          onArchive: _archiveExpenseReductionGoal,
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

  Future<void> _openPayYourselfFirst() async {
    final tenant = _tenant;
    if (tenant == null) return;
    await Navigator.of(context).push<void>(
      MaterialPageRoute(
        builder: (_) =>
            PayYourselfFirstPage(repository: _repository, tenantId: tenant.id),
      ),
    );
  }

  Future<void> _openNotifications() async {
    final tenant = _tenant;
    if (tenant == null) return;
    await Navigator.of(context).push<void>(
      MaterialPageRoute(
        builder: (_) => FinancialNotificationsPage(
          repository: _repository,
          tenantId: tenant.id,
        ),
      ),
    );
  }

  Future<void> _openPlanAndFeatures() => Navigator.of(context).push<void>(
    MaterialPageRoute(
      builder: (_) => EntitlementsPage(authController: widget.authController),
    ),
  );

  Future<void> _openAccountSecurity() async {
    final session = widget.authController.session;
    await showModalBottomSheet<void>(
      context: context,
      builder: (context) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Segurança da conta',
                style: Theme.of(
                  context,
                ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
              ),
              const SizedBox(height: 16),
              const Text('Sua sessão está autenticada pelo Neon Auth.'),
              const SizedBox(height: 8),
              Text(
                'Identificador da sessão: ${session.subject}',
                style: const TextStyle(color: AppTheme.muted),
              ),
              const SizedBox(height: 16),
              OutlinedButton.icon(
                onPressed: () async {
                  Navigator.pop(context);
                  await _returnToLogin();
                },
                icon: const Icon(Icons.logout),
                label: const Text('Encerrar esta sessão'),
              ),
            ],
          ),
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
    final amount = await showDialog<num>(
      context: context,
      useRootNavigator: true,
      builder: (_) => GoalProgressDialog(goalName: goal.name),
    );
    if (amount == null || !mounted) return;
    await _mutate(
      () => _repository.addGoalProgress(
        tenantId: tenant.id,
        goal: goal,
        amount: amount,
      ),
    );
  }

  Future<void> _editGoal(GoalSummary goal) async {
    final tenant = _tenant;
    if (tenant == null || goal.id.startsWith('local-')) return;
    final input = await showGoalForm(
      context,
      initial: GoalInput(
        name: goal.name,
        targetAmount: goal.targetAmount,
        targetDate: goal.targetDate,
      ),
    );
    if (input == null || !mounted) return;
    await _mutate(
      () => _repository.updateGoal(
        tenantId: tenant.id,
        goal: goal,
        name: input.name,
        targetAmount: input.targetAmount,
        targetDate: input.targetDate,
      ),
    );
  }

  Future<void> _archiveGoal(GoalSummary goal) async {
    final tenant = _tenant;
    if (tenant == null || goal.id.startsWith('local-')) return;
    await _mutate(
      () => _repository.archiveGoal(
        tenantId: tenant.id,
        goal: goal,
        archive: goal.status != 'Archived',
      ),
    );
  }

  Future<void> _archiveExpenseReductionGoal(
    ExpenseReductionGoalSummary goal,
  ) async {
    final tenant = _tenant;
    if (tenant == null || goal.id.startsWith('local-')) return;
    await _mutate(
      () => _repository.archiveExpenseReductionGoal(
        tenantId: tenant.id,
        goal: goal,
        archive: !goal.archived,
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
    final accounts = await _accountsForNewFinancialOperation(tenant.id);
    if (!mounted || accounts == null) return;
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
        occurredAt: input.occurredAt,
      ),
    );
  }

  Future<void> _createTransfer() async {
    final tenant = _tenant;
    if (tenant == null) return;
    final accounts = await _accountsForNewFinancialOperation(tenant.id);
    if (!mounted || accounts == null) return;
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
        occurredAt: input.occurredAt,
      ),
    );
  }

  Future<void> _cancelTransaction(
    TransactionSummary transaction,
    AccountSummary account,
  ) async {
    final tenant = _tenant;
    if (tenant == null || transaction.id.startsWith('local-')) return;
    await _mutate(
      () => _repository.cancelTransaction(
        tenantId: tenant.id,
        transaction: transaction,
        account: account,
      ),
    );
  }

  Future<List<AccountSummary>> _activeAccounts(String tenantId) async {
    try {
      final accounts = await _repository.accounts(tenantId);
      if (mounted) {
        setState(() => _accounts = accounts);
      }
      return accounts
          .where((item) => !item.archived && !item.id.startsWith('local-'))
          .toList(growable: false);
    } on ApiException catch (error) {
      if (mounted) _notice(_messageFor(error));
      return _accounts
          .where((item) => !item.archived && !item.id.startsWith('local-'))
          .toList(growable: false);
    }
  }

  Future<List<AccountSummary>?> _accountsForNewFinancialOperation(
    String tenantId,
  ) async {
    var accounts = await _activeAccounts(tenantId);
    if (!mounted || accounts.isNotEmpty) return accounts;

    final hasPendingAccount = _accounts.any(
      (item) => !item.archived && item.id.startsWith('local-'),
    );
    if (!hasPendingAccount) return accounts;

    final result = await _repository.synchronize(tenantId);
    if (!mounted) return null;
    if (result == FinanceSyncResult.synchronized) {
      accounts = await _activeAccounts(tenantId);
      if (!mounted) return null;
      if (accounts.isNotEmpty) return accounts;
    }

    _notice(switch (result) {
      FinanceSyncResult.offline =>
        'Sua conta está salva neste dispositivo e aguarda conexão para sincronizar.',
      FinanceSyncResult.unavailable =>
        'O servidor financeiro está indisponível no momento. Sua conta permanece protegida neste dispositivo.',
      FinanceSyncResult.blocked =>
        'Sua conta aguarda uma operação anterior que precisa de revisão na sincronização.',
      FinanceSyncResult.failed =>
        'A sincronização já está em andamento. Aguarde alguns instantes e tente novamente.',
      FinanceSyncResult.synchronized =>
        'A conta foi sincronizada, mas não ficou disponível neste espaço financeiro. Atualize e tente novamente.',
    });
    return null;
  }

  Future<void> _editTransaction(
    TransactionSummary transaction,
    AccountSummary account,
  ) async {
    final tenant = _tenant;
    if (tenant == null || transaction.id.startsWith('local-')) return;
    final input = await _showTransactionEditForm(context, transaction);
    if (input == null || !mounted) return;
    await _mutate(
      () => _repository.editTransaction(
        tenantId: tenant.id,
        transaction: transaction,
        account: account,
        amount: input.amount,
        description: input.description,
        occurredAt: input.occurredAt,
      ),
    );
  }

  Future<void> _maintainAccount(AccountSummary account) async {
    final tenant = _tenant;
    if (tenant == null || account.id.startsWith('local-')) return;
    final action = await _showResourceActions(
      context,
      title: account.name,
      archiveLabel: account.archived ? 'Reativar conta' : 'Arquivar conta',
      renameLabel: 'Editar detalhes',
    );
    if (action == null || !mounted) return;
    if (action == _ResourceAction.rename) {
      final input = await showAccountForm(context, initial: account);
      if (input == null || !mounted) return;
      await _mutate(() async {
        await _repository.updateAccount(
          tenantId: tenant.id,
          account: account,
          name: input.name,
          overdraftLimit: input.overdraftLimit,
          accountType: input.accountType,
          subtype: input.subtype,
          institution: input.institution,
          includeInNetWorth: input.includeInNetWorth,
        );
        return FinanceWriteState.applied;
      });
      return;
    }
    await _mutate(() async {
      await _repository.maintainAccount(
        tenantId: tenant.id,
        account: account,
        name: account.name,
        archive: !account.archived,
      );
      return FinanceWriteState.applied;
    });
  }

  Future<void> _maintainCategory(CategorySummary category) async {
    final tenant = _tenant;
    if (tenant == null || category.id.startsWith('local-')) return;
    final action = await _showResourceActions(
      context,
      title: category.name,
      archiveLabel: category.archived
          ? 'Reativar categoria'
          : 'Arquivar categoria',
    );
    if (action == null || !mounted) return;
    if (action == _ResourceAction.rename) {
      final name = await _showRenameForm(
        context,
        category.name,
        'Renomear categoria',
      );
      if (name == null || !mounted) return;
      await _mutate(() async {
        await _repository.maintainCategory(
          tenantId: tenant.id,
          category: category,
          name: name,
          archive: category.archived,
        );
        return FinanceWriteState.applied;
      });
      return;
    }
    await _mutate(() async {
      await _repository.maintainCategory(
        tenantId: tenant.id,
        category: category,
        name: category.name,
        archive: !category.archived,
      );
      return FinanceWriteState.applied;
    });
  }

  Future<void> _mutate(Future<FinanceWriteState> Function() operation) async {
    if (_mutating) return;
    setState(() => _mutating = true);
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
    } finally {
      if (mounted) setState(() => _mutating = false);
    }
  }

  Future<void> _synchronizeAndLoad() async {
    final tenant = _tenant;
    try {
      if (tenant != null) {
        final result = await _repository.synchronize(tenant.id);
        if (mounted) {
          _notice(switch (result) {
            FinanceSyncResult.synchronized => 'Dados sincronizados.',
            FinanceSyncResult.offline =>
              'Não foi possível alcançar o servidor. Seus lançamentos pendentes permanecem protegidos neste dispositivo.',
            FinanceSyncResult.unavailable =>
              'O servidor financeiro está indisponível no momento. Seus lançamentos pendentes permanecem protegidos neste dispositivo.',
            FinanceSyncResult.blocked =>
              'Há uma operação que precisa de revisão antes de sincronizar.',
            FinanceSyncResult.failed => 'A sincronização já está em andamento.',
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
              onIncome: () => _createTransaction('Income'),
              onExpense: () => _createTransaction('Expense'),
              onTransfer: _createTransfer,
              onGoals: _openGoals,
            ),
            1 => _Transactions(
              items: _transactions,
              accounts: _accounts,
              onRefresh: _load,
              onCancel: _cancelTransaction,
              onEdit: _editTransaction,
            ),
            2 => _Accounts(
              items: _accounts,
              onRefresh: _load,
              onMaintain: _maintainAccount,
            ),
            _ => _Menu(
              tenant: _tenant!,
              canSwitch: _tenants.length > 1,
              onSwitch: _chooseTenant,
              onSignOut: widget.authController.signOut,
              onSynchronize: _synchronizeAndLoad,
              conflictCount: _conflicts.length,
              onReviewConflicts: _reviewConflicts,
              onCategories: _openCategories,
              onCreditCards: _openCreditCards,
              onGoals: _openGoals,
              onExpenseReductionGoals: _openExpenseReductionGoals,
              onNotifications: _openNotifications,
              onFinancialAssistant: _openFinancialAssistant,
              onPayYourselfFirst: _openPayYourselfFirst,
              onPlanAndFeatures: _openPlanAndFeatures,
              onSecurity: _openAccountSecurity,
            ),
          };
    return Scaffold(
      appBar: AppBar(
        title: _tab == 0 && _profile != null
            ? Text('Olá, ${_profile!.displayName}')
            : Text(titles[_tab]),
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
          _loading || _mutating || _tenant == null || (_tab != 1 && _tab != 2)
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
    required this.onIncome,
    required this.onExpense,
    required this.onTransfer,
    required this.onGoals,
  });
  final List<DashboardMetric> metrics;
  final Future<void> Function() onRefresh;
  final String? insight;
  final bool insightLoading;
  final Future<void> Function() onGenerateInsight;
  final Future<void> Function() onIncome;
  final Future<void> Function() onExpense;
  final Future<void> Function() onTransfer;
  final Future<void> Function() onGoals;

  @override
  Widget build(BuildContext context) {
    final metric = metrics.isEmpty ? null : metrics.first;
    final currency = metric?.currency ?? 'BRL';
    final netWorth = metric?.netWorth ?? 0;
    final income = metric?.income ?? 0;
    final expense = metric?.expense ?? 0;
    final savingsRate = metric?.savingsRate ?? 0;
    return RefreshIndicator(
      onRefresh: onRefresh,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 28),
        children: [
          _DashboardHero(
            value: _money(netWorth, currency),
            onIncome: onIncome,
            onExpense: onExpense,
            onTransfer: onTransfer,
            onGoals: onGoals,
          ),
          const SizedBox(height: 16),
          Row(
            children: [
              Expanded(
                child: _DashboardStat(
                  title: 'Receitas',
                  value: _money(income, currency),
                  icon: Icons.south_rounded,
                  color: AppTheme.income,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: _DashboardStat(
                  title: 'Despesas',
                  value: _money(expense, currency),
                  icon: Icons.north_rounded,
                  color: AppTheme.expense,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  const Icon(Icons.insights_outlined, color: Color(0xFFF59E0B)),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Taxa de economia',
                          style: TextStyle(fontWeight: FontWeight.w700),
                        ),
                        Text(
                          metric == null
                              ? 'Registre lançamentos para acompanhar.'
                              : '${(savingsRate * 100).toStringAsFixed(1).replaceAll('.', ',')}% no período',
                          style: const TextStyle(color: AppTheme.muted),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 24),
          _InsightCard(
            insight: insight,
            loading: insightLoading,
            onGenerate: onGenerateInsight,
          ),
        ],
      ),
    );
  }
}

class _DashboardHero extends StatelessWidget {
  const _DashboardHero({
    required this.value,
    required this.onIncome,
    required this.onExpense,
    required this.onTransfer,
    required this.onGoals,
  });
  final String value;
  final Future<void> Function() onIncome;
  final Future<void> Function() onExpense;
  final Future<void> Function() onTransfer;
  final Future<void> Function() onGoals;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(20),
    decoration: BoxDecoration(
      color: AppTheme.navy,
      borderRadius: BorderRadius.circular(24),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'PATRIMÔNIO LÍQUIDO',
          style: TextStyle(
            color: Color(0xFFCBD5E1),
            fontWeight: FontWeight.w700,
          ),
        ),
        const SizedBox(height: 8),
        Text(
          value,
          style: const TextStyle(
            color: Colors.white,
            fontSize: 30,
            fontWeight: FontWeight.w800,
          ),
        ),
        const Divider(color: Color(0xFF334155), height: 32),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            _QuickAction(
              icon: Icons.add_rounded,
              label: 'Receita',
              color: AppTheme.income,
              onTap: onIncome,
            ),
            _QuickAction(
              icon: Icons.remove_rounded,
              label: 'Despesa',
              color: AppTheme.expense,
              onTap: onExpense,
            ),
            _QuickAction(
              icon: Icons.swap_horiz_rounded,
              label: 'Transferir',
              color: const Color(0xFF38BDF8),
              onTap: onTransfer,
            ),
            _QuickAction(
              icon: Icons.bolt_outlined,
              label: 'Metas',
              color: const Color(0xFFFFCC15),
              onTap: onGoals,
            ),
          ],
        ),
      ],
    ),
  );
}

class _QuickAction extends StatelessWidget {
  const _QuickAction({
    required this.icon,
    required this.label,
    required this.color,
    required this.onTap,
  });
  final IconData icon;
  final String label;
  final Color color;
  final Future<void> Function() onTap;
  @override
  Widget build(BuildContext context) => Semantics(
    button: true,
    label: label,
    child: InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(16),
      child: SizedBox(
        width: 66,
        child: Column(
          children: [
            Container(
              width: 46,
              height: 46,
              decoration: BoxDecoration(
                color: Colors.white10,
                borderRadius: BorderRadius.circular(16),
              ),
              child: Icon(icon, color: color),
            ),
            const SizedBox(height: 6),
            Text(
              label,
              style: const TextStyle(color: Color(0xFFE2E8F0), fontSize: 11),
            ),
          ],
        ),
      ),
    ),
  );
}

class _DashboardStat extends StatelessWidget {
  const _DashboardStat({
    required this.title,
    required this.value,
    required this.icon,
    required this.color,
  });
  final String title;
  final String value;
  final IconData icon;
  final Color color;
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: color),
          const SizedBox(height: 12),
          Text(
            title,
            style: const TextStyle(
              color: AppTheme.muted,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            value,
            style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800),
          ),
        ],
      ),
    ),
  );
}

class _InsightCard extends StatelessWidget {
  const _InsightCard({
    required this.insight,
    required this.loading,
    required this.onGenerate,
  });
  final String? insight;
  final bool loading;
  final Future<void> Function() onGenerate;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(20),
    decoration: BoxDecoration(
      color: const Color(0xFF111C42),
      borderRadius: BorderRadius.circular(20),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Row(
          children: [
            Icon(Icons.auto_awesome, color: Color(0xFFA5B4FC)),
            SizedBox(width: 8),
            Text(
              'INSIGHT FINANCEIRO COM IA',
              style: TextStyle(
                color: Color(0xFFA5B4FC),
                fontWeight: FontWeight.w800,
              ),
            ),
          ],
        ),
        const SizedBox(height: 14),
        Text(
          insight ??
              'Gere uma análise com os dados financeiros autorizados deste período.',
          style: const TextStyle(color: Colors.white, height: 1.4),
        ),
        const SizedBox(height: 16),
        FilledButton.icon(
          onPressed: loading ? null : onGenerate,
          style: FilledButton.styleFrom(backgroundColor: AppTheme.blue),
          icon: loading
              ? const SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(
                    strokeWidth: 2,
                    color: Colors.white,
                  ),
                )
              : const Icon(Icons.auto_awesome_outlined),
          label: Text(loading ? 'Gerando insight...' : 'Gerar insight do mês'),
        ),
      ],
    ),
  );
}

class _Transactions extends StatefulWidget {
  const _Transactions({
    required this.items,
    required this.accounts,
    required this.onRefresh,
    required this.onCancel,
    required this.onEdit,
  });
  final List<TransactionSummary> items;
  final List<AccountSummary> accounts;
  final Future<void> Function() onRefresh;
  final Future<void> Function(TransactionSummary, AccountSummary) onCancel;
  final Future<void> Function(TransactionSummary, AccountSummary) onEdit;

  @override
  State<_Transactions> createState() => _TransactionsState();
}

class _TransactionsState extends State<_Transactions> {
  late DateTime _month = DateTime(DateTime.now().year, DateTime.now().month);
  String _flow = 'All';
  String _search = '';
  bool _ascending = false;

  @override
  Widget build(BuildContext context) {
    final visible =
        widget.items.where((item) {
          final sameMonth =
              item.occurredAt.year == _month.year &&
              item.occurredAt.month == _month.month;
          final matchesFlow =
              _flow == 'All' ||
              (_flow == 'Transfer'
                  ? item.isTransfer
                  : item.flow.toLowerCase() == _flow.toLowerCase());
          final matchesSearch =
              _search.isEmpty ||
              item.description.toLowerCase().contains(_search.toLowerCase());
          return sameMonth && matchesFlow && matchesSearch;
        }).toList()..sort((a, b) {
          final byDate = a.occurredAt.compareTo(b.occurredAt);
          final stable = byDate == 0 ? a.id.compareTo(b.id) : byDate;
          return _ascending ? stable : -stable;
        });
    final income = visible
        .where((item) => item.flow == 'Income' && item.status == 'Posted')
        .fold<num>(0, (sum, item) => sum + item.amount);
    final expense = visible
        .where((item) => item.flow == 'Expense' && item.status == 'Posted')
        .fold<num>(0, (sum, item) => sum + item.amount);

    if (widget.items.isEmpty) {
      return _MessageState(
        icon: Icons.receipt_long_outlined,
        title: 'Nenhuma transação encontrada',
        message: 'As transações lançadas aparecerão aqui.',
        action: 'Atualizar',
        onAction: widget.onRefresh,
      );
    }
    return RefreshIndicator(
      onRefresh: widget.onRefresh,
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: visible.length + 1,
        itemBuilder: (context, index) {
          if (index == 0) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    IconButton(
                      onPressed: () => setState(
                        () => _month = DateTime(_month.year, _month.month - 1),
                      ),
                      icon: const Icon(Icons.chevron_left),
                    ),
                    Expanded(
                      child: Text(
                        _monthLabel(_month),
                        textAlign: TextAlign.center,
                        style: Theme.of(context).textTheme.titleMedium
                            ?.copyWith(fontWeight: FontWeight.w800),
                      ),
                    ),
                    IconButton(
                      onPressed: () => setState(
                        () => _month = DateTime(_month.year, _month.month + 1),
                      ),
                      icon: const Icon(Icons.chevron_right),
                    ),
                    IconButton(
                      tooltip: _ascending
                          ? 'Mais recentes primeiro'
                          : 'Mais antigas primeiro',
                      onPressed: () => setState(() => _ascending = !_ascending),
                      icon: Icon(
                        _ascending ? Icons.arrow_upward : Icons.arrow_downward,
                      ),
                    ),
                  ],
                ),
                _TransactionMonthSummary(income: income, expense: expense),
                const SizedBox(height: 12),
                TextField(
                  onChanged: (value) => setState(() => _search = value.trim()),
                  decoration: const InputDecoration(
                    prefixIcon: Icon(Icons.search),
                    hintText: 'Buscar por descrição...',
                  ),
                ),
                const SizedBox(height: 10),
                SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: Row(
                    children: [
                      for (final filter in const [
                        ('All', 'Todas'),
                        ('Income', 'Receitas'),
                        ('Expense', 'Despesas'),
                        ('Transfer', 'Transferências'),
                      ])
                        Padding(
                          padding: const EdgeInsets.only(right: 8),
                          child: ChoiceChip(
                            label: Text(filter.$2),
                            selected: _flow == filter.$1,
                            onSelected: (_) =>
                                setState(() => _flow = filter.$1),
                          ),
                        ),
                    ],
                  ),
                ),
                const SizedBox(height: 14),
                if (visible.isEmpty)
                  const Padding(
                    padding: EdgeInsets.symmetric(vertical: 44),
                    child: Center(
                      child: Text('Nenhuma movimentação neste período.'),
                    ),
                  ),
              ],
            );
          }
          final item = visible[index - 1];
          final previous = index > 1 ? visible[index - 2] : null;
          final newDay =
              previous == null ||
              !_sameFinancialDay(previous.occurredAt, item.occurredAt);
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (newDay)
                Padding(
                  padding: const EdgeInsets.fromLTRB(2, 14, 2, 8),
                  child: Text(
                    _dayLabel(item.occurredAt),
                    style: const TextStyle(
                      fontWeight: FontWeight.w800,
                      color: AppTheme.muted,
                    ),
                  ),
                ),
              _transactionTile(context, item),
            ],
          );
        },
      ),
    );
  }

  Widget _transactionTile(BuildContext context, TransactionSummary item) {
    final transfer = item.isTransfer;
    final income = item.flow.toLowerCase() == 'income';
    String? sourceName;
    String? destinationName;
    for (final account in widget.accounts) {
      if (account.id == (item.sourceAccountId ?? item.accountId)) {
        sourceName = account.name;
      }
      if (account.id == item.destinationAccountId) {
        destinationName = account.name;
      }
    }
    return Card(
      child: ListTile(
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
        leading: CircleAvatar(
          backgroundColor: transfer
              ? const Color(0xFFE7F0FF)
              : income
              ? const Color(0xFFDDF7E8)
              : const Color(0xFFFDE8E7),
          child: Icon(
            transfer
                ? Icons.swap_horiz
                : income
                ? Icons.south_west
                : Icons.north_east,
            color: transfer
                ? AppTheme.blue
                : income
                ? const Color(0xFF00875A)
                : const Color(0xFFD92D20),
          ),
        ),
        title: Text(
          item.description.isEmpty ? 'Transação' : item.description,
          style: const TextStyle(fontWeight: FontWeight.w800),
        ),
        subtitle: Text(
          transfer && destinationName != null
              ? '${sourceName ?? 'Origem'} -> $destinationName · ${_date(item.occurredAt)} · ${item.status}'
              : '${_date(item.occurredAt)} · ${item.status}',
        ),
        trailing: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              '${transfer
                  ? ''
                  : income
                  ? '+'
                  : '-'}${_money(item.amount, item.currency)}',
              style: TextStyle(
                fontWeight: FontWeight.w800,
                color: transfer
                    ? AppTheme.blue
                    : income
                    ? const Color(0xFF087A36)
                    : const Color(0xFFD92D20),
              ),
            ),
            Text(
              transfer
                  ? 'TRANSFERÊNCIA'
                  : income
                  ? 'RECEITA'
                  : 'DESPESA',
              style: TextStyle(
                fontSize: 10,
                fontWeight: FontWeight.w800,
                color: transfer
                    ? AppTheme.blue
                    : income
                    ? const Color(0xFF087A36)
                    : const Color(0xFFD92D20),
              ),
            ),
          ],
        ),
        onTap: () async {
          AccountSummary? account;
          for (final candidate in widget.accounts) {
            if (candidate.id == item.accountId) {
              account = candidate;
              break;
            }
          }
          final action = await _showTransactionDetails(
            context,
            item,
            canModify: !transfer && account != null,
            sourceName: sourceName,
            destinationName: destinationName,
          );
          if (!context.mounted || account == null) return;
          switch (action) {
            case _TransactionDetailAction.edit:
              await widget.onEdit(item, account);
            case _TransactionDetailAction.cancel:
              await widget.onCancel(item, account);
            case null:
              break;
          }
        },
      ),
    );
  }
}

class _TransactionMonthSummary extends StatelessWidget {
  const _TransactionMonthSummary({required this.income, required this.expense});
  final num income;
  final num expense;

  @override
  Widget build(BuildContext context) {
    final net = income - expense;
    return Container(
      margin: const EdgeInsets.only(top: 8),
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: const Color(0xFF111C42),
        borderRadius: BorderRadius.circular(22),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'SALDO DO MÊS',
            style: TextStyle(
              color: Color(0xFFB7C4E0),
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            _money(net, 'BRL'),
            style: const TextStyle(
              color: Colors.white,
              fontSize: 28,
              fontWeight: FontWeight.w800,
            ),
          ),
          const Divider(color: Color(0xFF405070), height: 28),
          Row(
            children: [
              Expanded(
                child: _SummaryValue(
                  label: 'ENTRADAS',
                  value: income,
                  color: const Color(0xFF39D7A5),
                ),
              ),
              Expanded(
                child: _SummaryValue(
                  label: 'SAÍDAS',
                  value: expense,
                  color: const Color(0xFFFF7185),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _SummaryValue extends StatelessWidget {
  const _SummaryValue({
    required this.label,
    required this.value,
    required this.color,
  });
  final String label;
  final num value;
  final Color color;
  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Text(
        label,
        style: const TextStyle(
          color: Color(0xFFB7C4E0),
          fontWeight: FontWeight.w700,
          fontSize: 12,
        ),
      ),
      const SizedBox(height: 4),
      Text(
        _money(value, 'BRL'),
        style: TextStyle(color: color, fontWeight: FontWeight.w800),
      ),
    ],
  );
}

enum _TransactionDetailAction { edit, cancel }

Future<_TransactionDetailAction?> _showTransactionDetails(
  BuildContext context,
  TransactionSummary transaction, {
  required bool canModify,
  String? sourceName,
  String? destinationName,
}) => showModalBottomSheet<_TransactionDetailAction>(
  context: context,
  useRootNavigator: true,
  builder: (context) {
    final income = transaction.flow.toLowerCase() == 'income';
    final transfer = transaction.isTransfer;
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
              label: transfer
                  ? 'Transferência'
                  : income
                  ? 'Receita'
                  : 'Despesa',
              value:
                  '${transfer
                      ? ''
                      : income
                      ? '+'
                      : '-'}${_money(transaction.amount, transaction.currency)}',
            ),
            if (transfer) ...[
              _DetailRow(
                label: 'Origem',
                value: sourceName ?? 'Conta de origem',
              ),
              _DetailRow(
                label: 'Destino',
                value: destinationName ?? 'Conta de destino',
              ),
            ],
            _DetailRow(label: 'Data', value: _date(transaction.occurredAt)),
            _DetailRow(label: 'Status', value: transaction.status),
            if (canModify && transaction.status != 'Cancelled') ...[
              const SizedBox(height: 12),
              FilledButton.tonalIcon(
                onPressed: () => Navigator.of(
                  context,
                  rootNavigator: true,
                ).pop(_TransactionDetailAction.edit),
                icon: const Icon(Icons.edit_outlined),
                label: const Text('Editar valor e descrição'),
              ),
            ],
            if (canModify && transaction.status != 'Cancelled') ...[
              const SizedBox(height: 12),
              OutlinedButton.icon(
                onPressed: () async {
                  final confirmed = await showDialog<bool>(
                    context: context,
                    useRootNavigator: true,
                    builder: (dialog) => AlertDialog(
                      title: const Text('Estornar lançamento?'),
                      content: const Text(
                        'O valor será estornado e o histórico será preservado.',
                      ),
                      actions: [
                        TextButton(
                          onPressed: () => Navigator.of(
                            dialog,
                            rootNavigator: true,
                          ).pop(false),
                          child: const Text('Voltar'),
                        ),
                        FilledButton(
                          onPressed: () => Navigator.of(
                            dialog,
                            rootNavigator: true,
                          ).pop(true),
                          child: const Text('Voltar'),
                        ),
                      ],
                    ),
                  );
                  if (confirmed == true) {
                    if (context.mounted) {
                      Navigator.of(
                        context,
                        rootNavigator: true,
                      ).pop(_TransactionDetailAction.cancel);
                    }
                  }
                },
                icon: const Icon(Icons.cancel_outlined),
                label: const Text('Estornar lançamento'),
              ),
            ],
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

class _Accounts extends StatefulWidget {
  const _Accounts({
    required this.items,
    required this.onRefresh,
    required this.onMaintain,
  });
  final List<AccountSummary> items;
  final Future<void> Function() onRefresh;
  final Future<void> Function(AccountSummary) onMaintain;

  @override
  State<_Accounts> createState() => _AccountsState();
}

class _AccountsState extends State<_Accounts> {
  bool _showArchived = false;

  @override
  Widget build(BuildContext context) {
    final visible = widget.items
        .where((item) => _showArchived || !item.archived)
        .toList(growable: false);
    final archivedCount = widget.items.where((item) => item.archived).length;
    if (visible.isEmpty && archivedCount == 0) {
      return _MessageState(
        icon: Icons.account_balance_wallet_outlined,
        title: 'Você ainda não tem contas',
        message:
            'Cadastre uma conta para começar a organizar sua vida financeira.',
        action: 'Atualizar',
        onAction: widget.onRefresh,
      );
    }
    return RefreshIndicator(
      onRefresh: widget.onRefresh,
      child: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: visible.length + 1,
        separatorBuilder: (_, _) => const SizedBox(height: 10),
        itemBuilder: (context, index) {
          if (index == 0) {
            return ListTile(
              contentPadding: EdgeInsets.zero,
              leading: Icon(
                _showArchived ? Icons.inventory_2 : Icons.inventory_2_outlined,
              ),
              title: Text(
                _showArchived
                    ? 'Ocultar contas arquivadas'
                    : 'Mostrar contas arquivadas',
              ),
              subtitle: Text(
                '$archivedCount ${archivedCount == 1 ? 'conta arquivada' : 'contas arquivadas'}',
              ),
              trailing: Icon(
                _showArchived
                    ? Icons.visibility_off_outlined
                    : Icons.visibility_outlined,
              ),
              onTap: () => setState(() => _showArchived = !_showArchived),
            );
          }
          final item = visible[index - 1];
          return Card(
            child: ListTile(
              leading: const Icon(Icons.account_balance_outlined),
              title: Text(item.name),
              subtitle: Text(
                item.archived
                    ? 'Conta arquivada'
                    : item.overdraftLimit > 0
                    ? '${_accountLabel(item)} · Cheque especial: ${_money(item.overdraftLimit, item.currency)}'
                    : '${_accountLabel(item)}${item.includeInNetWorth ? '' : ' · Fora do patrimônio'}',
              ),
              trailing: Text(
                _money(item.balance, item.currency),
                style: Theme.of(
                  context,
                ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800),
              ),
              onTap: () => widget.onMaintain(item),
            ),
          );
        },
      ),
    );
  }
}

String _accountLabel(AccountSummary account) {
  final type = switch (account.accountType) {
    'Wallet' => 'Carteira',
    'Investment' => 'Investimento',
    _ => 'Banco',
  };
  final detail = account.institution ?? account.subtype;
  return detail == null || detail.isEmpty ? type : '$type · $detail';
}

class _ExpenseReductionGoalsPage extends StatefulWidget {
  const _ExpenseReductionGoalsPage({
    required this.goals,
    required this.categories,
    required this.onRefresh,
    required this.onOpportunities,
    required this.onCreate,
    required this.onArchive,
  });
  final List<ExpenseReductionGoalSummary> goals;
  final List<CategorySummary> categories;
  final Future<List<ExpenseReductionGoalSummary>> Function() onRefresh;
  final Future<List<ExpenseReductionOpportunitySummary>> Function(int months)
  onOpportunities;
  final Future<void> Function(String? categoryId, num reference, num target)
  onCreate;
  final Future<void> Function(ExpenseReductionGoalSummary goal) onArchive;

  @override
  State<_ExpenseReductionGoalsPage> createState() =>
      _ExpenseReductionGoalsPageState();
}

class _ExpenseReductionGoalsPageState
    extends State<_ExpenseReductionGoalsPage> {
  late List<ExpenseReductionGoalSummary> _goals = widget.goals;
  List<ExpenseReductionOpportunitySummary> _opportunities = const [];
  bool _loadingOpportunities = false;
  String? _opportunitiesError;
  int _historicalMonths = 3;
  bool _showArchived = false;

  Future<void> _refresh() async {
    try {
      final goals = await widget.onRefresh();
      if (mounted) setState(() => _goals = goals);
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Não foi possível atualizar as metas.')),
        );
      }
    }
    if (mounted) await _loadOpportunities();
  }

  @override
  void initState() {
    super.initState();
    _loadOpportunities();
  }

  Future<void> _loadOpportunities() async {
    final months = _historicalMonths;
    setState(() => _loadingOpportunities = true);
    try {
      final opportunities = await widget.onOpportunities(months);
      if (mounted) {
        setState(() {
          _opportunities = opportunities;
          _opportunitiesError = null;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(
          () => _opportunitiesError =
              'Não foi possível atualizar as oportunidades agora.',
        );
      }
    } finally {
      if (mounted) setState(() => _loadingOpportunities = false);
    }
  }

  Future<void> _create() async {
    final reference = TextEditingController();
    final target = TextEditingController();
    String? categoryId;
    final values = await showDialog<(String?, num, num)>(
      context: context,
      useRootNavigator: true,
      builder: (context) => AlertDialog(
        title: const Text('Meta de redução mensal'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            DropdownButtonFormField<String?>(
              initialValue: categoryId,
              decoration: const InputDecoration(
                labelText: 'Categoria (opcional)',
              ),
              items: [
                const DropdownMenuItem<String?>(
                  value: null,
                  child: Text('Todas as despesas'),
                ),
                ...widget.categories.map(
                  (category) => DropdownMenuItem<String?>(
                    value: category.id,
                    child: Text(category.name),
                  ),
                ),
              ],
              onChanged: (value) => categoryId = value,
            ),
            const SizedBox(height: 12),
            TextField(
              controller: reference,
              keyboardType: const TextInputType.numberWithOptions(
                decimal: true,
              ),
              decoration: const InputDecoration(
                labelText: 'Gasto de referência (R\$)',
              ),
            ),
            TextField(
              controller: target,
              keyboardType: const TextInputType.numberWithOptions(
                decimal: true,
              ),
              decoration: const InputDecoration(
                labelText: 'Limite desejado (R\$)',
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context, rootNavigator: true).pop(),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () {
              final from = num.tryParse(reference.text.replaceAll(',', '.'));
              final to = num.tryParse(target.text.replaceAll(',', '.'));
              if (from == null ||
                  to == null ||
                  from <= 0 ||
                  to < 0 ||
                  to >= from) {
                return;
              }
              Navigator.of(
                context,
                rootNavigator: true,
              ).pop((categoryId, from, to));
            },
            child: const Text('Criar'),
          ),
        ],
      ),
    );
    reference.dispose();
    target.dispose();
    if (values == null) return;
    await widget.onCreate(values.$1, values.$2, values.$3);
    await _refresh();
  }

  Future<void> _archive(ExpenseReductionGoalSummary goal) async {
    final restoring = goal.archived;
    final confirmed = await showDialog<bool>(
      context: context,
      useRootNavigator: true,
      builder: (context) => AlertDialog(
        title: Text(
          restoring ? 'Reativar meta de redução?' : 'Cancelar meta de redução?',
        ),
        content: Text(
          restoring
              ? 'A meta voltará a participar do acompanhamento mensal.'
              : 'A meta será arquivada e não será apagada.',
        ),
        actions: [
          TextButton(
            onPressed: () =>
                Navigator.of(context, rootNavigator: true).pop(false),
            child: const Text('Voltar'),
          ),
          FilledButton(
            onPressed: () =>
                Navigator.of(context, rootNavigator: true).pop(true),
            child: Text(restoring ? 'Reativar meta' : 'Cancelar meta'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;
    await widget.onArchive(goal);
    await _refresh();
  }

  @override
  Widget build(BuildContext context) {
    final visible = _goals
        .where((goal) => _showArchived || !goal.archived)
        .toList(growable: false);
    final hasOpportunityError = _opportunitiesError != null;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Redução de despesas'),
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(84),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: DropdownButtonFormField<int>(
              initialValue: _historicalMonths,
              decoration: const InputDecoration(
                labelText: 'Meses por período comparado',
                helperText: 'Somente despesas realizadas; previsões excluídas.',
              ),
              items: List.generate(
                12,
                (index) => DropdownMenuItem(
                  value: index + 1,
                  child: Text('${index + 1} mês(es)'),
                ),
              ),
              onChanged: _loadingOpportunities
                  ? null
                  : (months) {
                      if (months == null) return;
                      setState(() {
                        _historicalMonths = months;
                        _opportunities = const [];
                      });
                      _loadOpportunities();
                    },
            ),
          ),
        ),
        actions: [
          IconButton(
            tooltip: _showArchived ? 'Ocultar arquivadas' : 'Exibir arquivadas',
            onPressed: () => setState(() => _showArchived = !_showArchived),
            icon: Icon(
              _showArchived ? Icons.inventory_2 : Icons.inventory_2_outlined,
            ),
          ),
          IconButton(
            onPressed: _refresh,
            tooltip: 'Atualizar',
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: _create,
        tooltip: 'Criar meta de redução',
        child: const Icon(Icons.add),
      ),
      body:
          visible.isEmpty &&
              _opportunities.isEmpty &&
              !_loadingOpportunities &&
              _opportunitiesError == null
          ? _MessageState(
              icon: Icons.trending_down_outlined,
              title: 'Nenhuma meta de redução criada',
              message: 'Crie uma meta para acompanhar sua economia mensal.',
              action: 'Atualizar',
              onAction: _refresh,
            )
          : ListView.separated(
              padding: const EdgeInsets.all(16),
              itemCount:
                  visible.length +
                  _opportunities.length +
                  1 +
                  (hasOpportunityError ? 1 : 0),
              separatorBuilder: (_, _) => const SizedBox(height: 12),
              itemBuilder: (_, index) {
                if (index == 0) {
                  final planned = visible.fold<num>(
                    0,
                    (sum, item) => sum + item.targetAmount,
                  );
                  final actual = visible.fold<num>(
                    0,
                    (sum, item) => sum + item.actualExpense,
                  );
                  return _DarkSummaryCard(
                    eyebrow: 'REDUÇÃO DE DESPESAS',
                    value: _money(planned, 'BRL'),
                    detail: actual <= planned
                        ? 'Dentro da meta planejada'
                        : 'Acima da meta planejada',
                    icon: Icons.trending_down_outlined,
                  );
                }
                if (hasOpportunityError && index == 1) {
                  return Card(
                    child: ListTile(
                      leading: const Icon(Icons.cloud_off_outlined),
                      title: Text(_opportunitiesError!),
                      subtitle: const Text(
                        'As metas manuais continuam disponíveis e nenhum dado foi alterado.',
                      ),
                    ),
                  );
                }
                final opportunityIndex =
                    index - 1 - (hasOpportunityError ? 1 : 0);
                if (opportunityIndex < _opportunities.length) {
                  final opportunity = _opportunities[opportunityIndex];
                  return Card(
                    child: ListTile(
                      leading: const Icon(Icons.insights_outlined),
                      title: Text(opportunity.category),
                      subtitle: Text(
                        'Gasto recente: ${_money(opportunity.recentExpense, opportunity.currency)} · período anterior: ${_money(opportunity.previousExpense, opportunity.currency)}',
                      ),
                      trailing: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          const Text('Oportunidade'),
                          Text(
                            _money(
                              opportunity.suggestedReduction,
                              opportunity.currency,
                            ),
                            style: const TextStyle(fontWeight: FontWeight.w800),
                          ),
                        ],
                      ),
                    ),
                  );
                }
                final goal = visible[opportunityIndex - _opportunities.length];
                final remaining = (goal.targetAmount - goal.actualExpense)
                    .clamp(0, goal.targetAmount);
                return Card(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            const CircleAvatar(
                              child: Icon(Icons.savings_outlined),
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Text(
                                'Meta mensal de redução',
                                style: Theme.of(context).textTheme.titleMedium
                                    ?.copyWith(fontWeight: FontWeight.w800),
                              ),
                            ),
                            IconButton(
                              tooltip: goal.archived
                                  ? 'Reativar meta de redução'
                                  : 'Cancelar meta de redução',
                              onPressed: goal.id.startsWith('local-')
                                  ? null
                                  : () => _archive(goal),
                              icon: Icon(
                                goal.archived
                                    ? Icons.unarchive_outlined
                                    : Icons.archive_outlined,
                              ),
                            ),
                            Text(
                              '${(goal.progress * 100).round()}%',
                              style: const TextStyle(
                                fontWeight: FontWeight.w800,
                                color: Color(0xFF2563EB),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 14),
                        Text(
                          'Gasto atual ${_money(goal.actualExpense, goal.currency)} de ${_money(goal.targetAmount, goal.currency)}',
                        ),
                        const SizedBox(height: 8),
                        LinearProgressIndicator(value: goal.progress),
                        const SizedBox(height: 8),
                        Text(
                          'Ainda disponível no limite: ${_money(remaining, goal.currency)}',
                          style: const TextStyle(color: Color(0xFF64748B)),
                        ),
                      ],
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
    required this.onCreditCards,
    required this.onGoals,
    required this.onExpenseReductionGoals,
    required this.onNotifications,
    required this.onFinancialAssistant,
    required this.onPayYourselfFirst,
    required this.onPlanAndFeatures,
    required this.onSecurity,
  });
  final TenantSummary tenant;
  final bool canSwitch;
  final VoidCallback onSwitch;
  final Future<void> Function() onSignOut;
  final Future<void> Function() onSynchronize;
  final int conflictCount;
  final Future<void> Function() onReviewConflicts;
  final Future<void> Function() onCategories;
  final Future<void> Function() onCreditCards;
  final Future<void> Function() onGoals;
  final Future<void> Function() onExpenseReductionGoals;
  final Future<void> Function() onNotifications;
  final Future<void> Function() onFinancialAssistant;
  final Future<void> Function() onPayYourselfFirst;
  final Future<void> Function() onPlanAndFeatures;
  final Future<void> Function() onSecurity;

  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(16),
    children: [
      Card(
        child: ListTile(
          contentPadding: const EdgeInsets.all(16),
          leading: const CircleAvatar(child: Icon(Icons.person_outline)),
          title: Text(
            tenant.name,
            style: const TextStyle(fontWeight: FontWeight.w800),
          ),
          subtitle: const Text('Sessão ativa e protegida'),
          trailing: canSwitch ? const Icon(Icons.chevron_right) : null,
          onTap: canSwitch ? onSwitch : null,
        ),
      ),
      const SizedBox(height: 18),
      const Text(
        'FINANÇAS & GESTÃO',
        style: TextStyle(fontWeight: FontWeight.w800, color: Color(0xFF718096)),
      ),
      _MenuItem(
        leading: const Icon(Icons.category_outlined),
        title: const Text('Categorias'),
        subtitle: const Text('Organize receitas e despesas.'),
        trailing: const Icon(Icons.chevron_right),
        onTap: onCategories,
      ),
      _MenuItem(
        leading: const Icon(Icons.credit_card_outlined),
        title: const Text('Cartões de crédito'),
        subtitle: const Text('Faturas, limites e compras parceladas.'),
        trailing: const Icon(Icons.chevron_right),
        onTap: onCreditCards,
      ),
      _MenuItem(
        leading: const Icon(Icons.flag_outlined),
        title: const Text('Metas'),
        subtitle: const Text('Acompanhe objetivos financeiros.'),
        trailing: const Icon(Icons.chevron_right),
        onTap: onGoals,
      ),
      _MenuItem(
        leading: const Icon(Icons.trending_down_outlined),
        title: const Text('Redução de despesas'),
        subtitle: const Text(
          'Compare seu gasto atual com uma meta de economia.',
        ),
        trailing: const Icon(Icons.chevron_right),
        onTap: onExpenseReductionGoals,
      ),
      _MenuItem(
        leading: const Icon(Icons.auto_awesome_outlined),
        title: const Text('Assistente financeiro'),
        subtitle: const Text('Pergunte sobre dados autorizados.'),
        trailing: const Icon(Icons.chevron_right),
        onTap: onFinancialAssistant,
      ),
      _MenuItem(
        leading: const Icon(Icons.savings_outlined),
        title: const Text('Primeiro Me Pago'),
        subtitle: const Text('Planeje a reserva antes dos demais gastos.'),
        trailing: const Icon(Icons.chevron_right),
        onTap: onPayYourselfFirst,
      ),
      _MenuItem(
        leading: const Icon(Icons.notifications_outlined),
        title: const Text('Notificações'),
        subtitle: const Text('Alertas e avisos financeiros.'),
        trailing: const Icon(Icons.chevron_right),
        onTap: onNotifications,
      ),
      _MenuItem(
        leading: const Icon(Icons.workspace_premium_outlined),
        title: const Text('Plano e recursos'),
        subtitle: const Text('Consulte os recursos liberados para sua conta.'),
        trailing: const Icon(Icons.chevron_right),
        onTap: onPlanAndFeatures,
      ),
      _MenuItem(
        leading: const Icon(Icons.sync_outlined),
        title: const Text('Sincronização'),
        subtitle: const Text('Enviar lançamentos pendentes e atualizar dados.'),
        trailing: const Icon(Icons.chevron_right),
        onTap: onSynchronize,
      ),
      if (conflictCount > 0)
        _MenuItem(
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
      _MenuItem(
        leading: const Icon(Icons.security_outlined),
        title: const Text('Segurança da conta'),
        subtitle: const Text('Sessão, autenticação e encerramento.'),
        trailing: const Icon(Icons.chevron_right),
        onTap: onSecurity,
      ),
      const SizedBox(height: 18),
      ListTile(
        leading: const Icon(Icons.logout, color: Color(0xFFD92D20)),
        title: const Text('Sair', style: TextStyle(color: Color(0xFFD92D20))),
        onTap: () async => onSignOut(),
      ),
    ],
  );
}

class _MenuItem extends StatelessWidget {
  const _MenuItem({
    required this.leading,
    required this.title,
    required this.subtitle,
    this.trailing,
    this.onTap,
  });
  final Widget leading;
  final Widget title;
  final Widget subtitle;
  final Widget? trailing;
  final VoidCallback? onTap;
  @override
  Widget build(BuildContext context) => Card(
    margin: const EdgeInsets.only(top: 8),
    child: ListTile(
      leading: leading,
      title: title,
      subtitle: subtitle,
      trailing: trailing ?? const Icon(Icons.chevron_right),
      onTap: onTap,
    ),
  );
}

class _CategoriesPage extends StatefulWidget {
  const _CategoriesPage({
    required this.categories,
    required this.onCreate,
    required this.onRefresh,
    required this.onMaintain,
  });
  final List<CategorySummary> categories;
  final Future<void> Function() onCreate;
  final Future<List<CategorySummary>> Function() onRefresh;
  final Future<void> Function(CategorySummary) onMaintain;

  @override
  State<_CategoriesPage> createState() => _CategoriesPageState();
}

class _CategoriesPageState extends State<_CategoriesPage> {
  late List<CategorySummary> _categories = widget.categories;
  bool _loading = false;
  bool _showArchived = false;

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
    if (!mounted) return;
    await _refresh();
  }

  @override
  Widget build(BuildContext context) {
    final visible = _categories
        .where((item) => _showArchived || !item.archived)
        .toList(growable: false);
    return Scaffold(
      appBar: AppBar(
        title: const Text('Categorias'),
        actions: [
          IconButton(
            tooltip: _showArchived ? 'Ocultar arquivadas' : 'Exibir arquivadas',
            onPressed: () => setState(() => _showArchived = !_showArchived),
            icon: Icon(
              _showArchived ? Icons.inventory_2 : Icons.inventory_2_outlined,
            ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: _loading ? null : _create,
        tooltip: 'Nova categoria',
        child: const Icon(Icons.add),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : visible.isEmpty
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
                itemCount: visible.length,
                separatorBuilder: (_, _) => const Divider(height: 1),
                itemBuilder: (context, index) {
                  final category = visible[index];
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
                    subtitle: Text(
                      category.archived
                          ? 'Arquivada · ${income ? 'Receita' : 'Despesa'}'
                          : income
                          ? 'Receita'
                          : 'Despesa',
                    ),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () async {
                      await widget.onMaintain(category);
                      if (!mounted) return;
                      await _refresh();
                    },
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
    required this.onEdit,
    required this.onArchive,
  });

  final List<GoalSummary> goals;
  final Future<void> Function() onCreate;
  final Future<List<GoalSummary>> Function() onRefresh;
  final Future<void> Function(GoalSummary goal) onAddProgress;
  final Future<void> Function(GoalSummary goal) onEdit;
  final Future<void> Function(GoalSummary goal) onArchive;

  @override
  State<_GoalsPage> createState() => _GoalsPageState();
}

class _GoalsPageState extends State<_GoalsPage> {
  late List<GoalSummary> _goals = widget.goals;
  bool _loading = false;
  bool _showArchived = false;

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

  Future<void> _archive(GoalSummary goal) async {
    final restoring = goal.status == 'Archived';
    final confirmed = await showDialog<bool>(
      context: context,
      useRootNavigator: true,
      builder: (context) => AlertDialog(
        title: Text(restoring ? 'Reativar meta?' : 'Cancelar meta?'),
        content: Text(
          restoring
              ? 'A meta voltará a participar do acompanhamento.'
              : 'A meta será arquivada e seu histórico será preservado.',
        ),
        actions: [
          TextButton(
            onPressed: () =>
                Navigator.of(context, rootNavigator: true).pop(false),
            child: const Text('Voltar'),
          ),
          FilledButton(
            onPressed: () =>
                Navigator.of(context, rootNavigator: true).pop(true),
            child: Text(restoring ? 'Reativar meta' : 'Cancelar meta'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;
    await widget.onArchive(goal);
    await _refresh();
  }

  @override
  Widget build(BuildContext context) {
    final active = _goals.where((goal) => goal.status == 'Active').toList();
    final visible = _showArchived ? _goals : active;
    final summary = active.isEmpty ? visible : active;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Metas'),
        actions: [
          IconButton(
            tooltip: _showArchived ? 'Ocultar arquivadas' : 'Exibir arquivadas',
            onPressed: () => setState(() => _showArchived = !_showArchived),
            icon: Icon(
              _showArchived ? Icons.inventory_2 : Icons.inventory_2_outlined,
            ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: _loading ? null : _create,
        tooltip: 'Nova meta',
        child: const Icon(Icons.add),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : visible.isEmpty
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
                itemCount: visible.length + 1,
                separatorBuilder: (_, _) => const SizedBox(height: 10),
                itemBuilder: (context, index) {
                  if (index == 0) {
                    final total = summary.fold<num>(
                      0,
                      (sum, goal) => sum + goal.targetAmount,
                    );
                    final saved = summary.fold<num>(
                      0,
                      (sum, goal) => sum + goal.currentAmount,
                    );
                    return _DarkSummaryCard(
                      eyebrow: 'ECONOMIA CONSOLIDADA',
                      value: _money(saved, summary.first.currency),
                      detail:
                          'Objetivo total ${_money(total, summary.first.currency)}',
                      icon: Icons.flag_outlined,
                    );
                  }
                  final goal = visible[index - 1];
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
                                )
                              else
                                IconButton(
                                  tooltip: 'Editar meta',
                                  onPressed: () async {
                                    await widget.onEdit(goal);
                                    if (mounted) await _refresh();
                                  },
                                  icon: const Icon(Icons.edit_outlined),
                                ),
                              if (!localOnly)
                                IconButton(
                                  tooltip: goal.status == 'Archived'
                                      ? 'Reativar meta'
                                      : 'Cancelar meta',
                                  onPressed: () => _archive(goal),
                                  icon: Icon(
                                    goal.status == 'Archived'
                                        ? Icons.unarchive_outlined
                                        : Icons.archive_outlined,
                                  ),
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
                              onPressed: localOnly || goal.status == 'Archived'
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

class _DarkSummaryCard extends StatelessWidget {
  const _DarkSummaryCard({
    required this.eyebrow,
    required this.value,
    required this.detail,
    required this.icon,
  });
  final String eyebrow;
  final String value;
  final String detail;
  final IconData icon;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(20),
    decoration: BoxDecoration(
      color: const Color(0xFF111C42),
      borderRadius: BorderRadius.circular(20),
    ),
    child: Row(
      children: [
        CircleAvatar(
          backgroundColor: const Color(0xFF263761),
          child: Icon(icon, color: const Color(0xFF8CB4FF)),
        ),
        const SizedBox(width: 14),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                eyebrow,
                style: const TextStyle(
                  color: Color(0xFFA5B4FC),
                  fontWeight: FontWeight.w800,
                  fontSize: 12,
                ),
              ),
              const SizedBox(height: 6),
              Text(
                value,
                style: const TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.w800,
                  fontSize: 26,
                ),
              ),
              const SizedBox(height: 4),
              Text(detail, style: const TextStyle(color: Color(0xFFC7D2FE))),
            ],
          ),
        ),
      ],
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

enum _ResourceAction { rename, archive }

class _TransactionEditInput {
  const _TransactionEditInput({
    required this.amount,
    required this.description,
    required this.occurredAt,
  });
  final num amount;
  final String description;
  final DateTime occurredAt;
}

Future<_TransactionEditInput?> _showTransactionEditForm(
  BuildContext context,
  TransactionSummary transaction,
) {
  return showDialog<_TransactionEditInput>(
    context: context,
    useRootNavigator: true,
    builder: (_) => _TransactionEditDialog(transaction: transaction),
  );
}

class _TransactionEditDialog extends StatefulWidget {
  const _TransactionEditDialog({required this.transaction});

  final TransactionSummary transaction;

  @override
  State<_TransactionEditDialog> createState() => _TransactionEditDialogState();
}

class _TransactionEditDialogState extends State<_TransactionEditDialog> {
  late final TextEditingController _amount;
  late final TextEditingController _description;
  late DateTime _occurredAt;

  @override
  void initState() {
    super.initState();

    _amount = TextEditingController(
      text: widget.transaction.amount.toStringAsFixed(2).replaceAll('.', ','),
    );

    _description = TextEditingController(text: widget.transaction.description);

    _occurredAt = widget.transaction.occurredAt;
  }

  @override
  void dispose() {
    _amount.dispose();
    _description.dispose();
    super.dispose();
  }

  Future<void> _selectDate() async {
    final chosen = await showDatePicker(
      context: context,
      initialDate: _occurredAt,
      firstDate: DateTime(2000),
      lastDate: DateTime(DateTime.now().year + 30),
    );

    if (!mounted || chosen == null) return;

    setState(() {
      _occurredAt = chosen;
    });
  }

  void _save() {
    final parsedAmount = _parseAmount(_amount.text);

    if (parsedAmount == null || parsedAmount <= 0) {
      return;
    }

    Navigator.of(context).pop(
      _TransactionEditInput(
        amount: parsedAmount,
        description: _description.text.trim(),
        occurredAt: _occurredAt,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Editar lançamento'),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: _amount,
              autofocus: true,
              keyboardType: const TextInputType.numberWithOptions(
                decimal: true,
              ),
              decoration: const InputDecoration(labelText: 'Valor (R\$)'),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _description,
              textCapitalization: TextCapitalization.sentences,
              decoration: const InputDecoration(labelText: 'Descrição'),
            ),
            const SizedBox(height: 12),
            OutlinedButton.icon(
              onPressed: _selectDate,
              icon: const Icon(Icons.calendar_today_outlined),
              label: Text('Data da transação: ${_dateFull(_occurredAt)}'),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancelar'),
        ),
        FilledButton(onPressed: _save, child: const Text('Salvar')),
      ],
    );
  }
}

Future<_ResourceAction?> _showResourceActions(
  BuildContext context, {
  required String title,
  required String archiveLabel,
  String renameLabel = 'Renomear',
}) => showModalBottomSheet<_ResourceAction>(
  context: context,
  useRootNavigator: true,
  builder: (context) => SafeArea(
    child: Wrap(
      children: [
        ListTile(title: Text(title)),
        ListTile(
          leading: const Icon(Icons.edit_outlined),
          title: Text(renameLabel),
          onTap: () => Navigator.of(
            context,
            rootNavigator: true,
          ).pop(_ResourceAction.rename),
        ),
        ListTile(
          leading: const Icon(Icons.archive_outlined),
          title: Text(archiveLabel),
          onTap: () => Navigator.of(
            context,
            rootNavigator: true,
          ).pop(_ResourceAction.archive),
        ),
      ],
    ),
  ),
);

Future<String?> _showRenameForm(
  BuildContext context,
  String initialName,
  String title,
) async {
  final controller = TextEditingController(text: initialName);
  final result = await showDialog<String>(
    context: context,
    useRootNavigator: true,
    builder: (dialogContext) => AlertDialog(
      title: Text(title),
      content: TextField(
        controller: controller,
        autofocus: true,
        textCapitalization: TextCapitalization.words,
        decoration: const InputDecoration(labelText: 'Nome'),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(dialogContext),
          child: const Text('Cancelar'),
        ),
        FilledButton(
          onPressed: () {
            final name = controller.text.trim();
            if (name.isEmpty) return;
            Navigator.pop(dialogContext, name);
          },
          child: const Text('Salvar'),
        ),
      ],
    ),
  );
  controller.dispose();
  return result;
}

String _money(num value, String currency) {
  final prefix = currency == 'BRL' ? 'R\$ ' : '$currency ';
  return '$prefix${value.toStringAsFixed(2).replaceAll('.', ',')}';
}

String _date(DateTime value) =>
    '${value.day.toString().padLeft(2, '0')}/${value.month.toString().padLeft(2, '0')}';

String _dateFull(DateTime value) =>
    '${value.day.toString().padLeft(2, '0')}/${value.month.toString().padLeft(2, '0')}/${value.year}';

bool _sameFinancialDay(DateTime left, DateTime right) =>
    left.year == right.year &&
    left.month == right.month &&
    left.day == right.day;

String _dayLabel(DateTime value) =>
    '${_dateFull(value)}${_sameFinancialDay(value, DateTime.now()) ? ' · HOJE' : ''}';

String _monthLabel(DateTime value) {
  const months = [
    'Janeiro',
    'Fevereiro',
    'Março',
    'Abril',
    'Maio',
    'Junho',
    'Julho',
    'Agosto',
    'Setembro',
    'Outubro',
    'Novembro',
    'Dezembro',
  ];
  return '${months[value.month - 1]} ${value.year}';
}

num? _parseAmount(String value) {
  var normalized = value.trim();
  if (normalized.contains(',') && normalized.contains('.')) {
    normalized = normalized.replaceAll('.', '').replaceAll(',', '.');
  } else {
    normalized = normalized.replaceAll(',', '.');
  }
  return num.tryParse(normalized);
}
