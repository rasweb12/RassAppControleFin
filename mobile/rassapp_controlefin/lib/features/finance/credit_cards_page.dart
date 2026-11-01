import 'package:flutter/material.dart';

import 'finance_repository.dart';

class CreditCardsPage extends StatefulWidget {
  const CreditCardsPage({
    required this.repository,
    required this.tenantId,
    required this.accounts,
    required this.categories,
    super.key,
  });

  final FinanceRepository repository;
  final String tenantId;
  final List<AccountSummary> accounts;
  final List<CategorySummary> categories;

  @override
  State<CreditCardsPage> createState() => _CreditCardsPageState();
}

class _CreditCardsPageState extends State<CreditCardsPage> {
  List<CreditCardSummary> _cards = const [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final cards = await widget.repository.creditCards(widget.tenantId);
      if (mounted) setState(() => _cards = cards);
    } catch (_) {
      if (mounted) _error = 'Não foi possível carregar os cartões.';
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _createCard() async {
    final input = await _showCardForm(context);
    if (input == null || !mounted) return;
    try {
      final state = await widget.repository.createCreditCard(
        tenantId: widget.tenantId,
        name: input.name,
        limit: input.limit,
        closingDay: input.closingDay,
        dueDay: input.dueDay,
        institution: input.institution,
        brand: input.brand,
        preferredPaymentAccountId: input.preferredPaymentAccountId,
        accentColor: input.accentColor,
      );
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            state == FinanceWriteState.pending
                ? 'Cartão salvo neste dispositivo e aguardando sincronização.'
                : 'Cartão criado.',
          ),
        ),
      );
      await _load();
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Não foi possível criar o cartão.')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('Cartões de crédito'),
      actions: [
        IconButton(
          onPressed: _loading ? null : _load,
          tooltip: 'Atualizar',
          icon: const Icon(Icons.refresh),
        ),
      ],
    ),
    floatingActionButton: FloatingActionButton.extended(
      onPressed: _createCard,
      icon: const Icon(Icons.add_card_outlined),
      label: const Text('Novo cartão'),
    ),
    body: _loading
        ? const Center(child: CircularProgressIndicator())
        : _error != null
        ? _RetryState(message: _error!, onRetry: _load)
        : _cards.isEmpty
        ? _RetryState(
            message: 'Nenhum cartão cadastrado.',
            detail: 'Cadastre o limite e acompanhe suas próximas faturas.',
            onRetry: _load,
          )
        : RefreshIndicator(
            onRefresh: _load,
            child: ListView.separated(
              padding: const EdgeInsets.all(16),
              itemCount: _cards.length,
              separatorBuilder: (_, _) => const SizedBox(height: 12),
              itemBuilder: (_, index) {
                final card = _cards[index];
                return _CreditCardTile(
                  card: card,
                  onTap: () async {
                    await Navigator.of(context).push<void>(
                      MaterialPageRoute(
                        builder: (_) => CreditCardDetailPage(
                          repository: widget.repository,
                          tenantId: widget.tenantId,
                          card: card,
                          accounts: widget.accounts,
                          categories: widget.categories,
                        ),
                      ),
                    );
                    if (mounted) await _load();
                  },
                );
              },
            ),
          ),
  );
}

class CreditCardDetailPage extends StatefulWidget {
  const CreditCardDetailPage({
    required this.repository,
    required this.tenantId,
    required this.card,
    required this.accounts,
    required this.categories,
    super.key,
  });

  final FinanceRepository repository;
  final String tenantId;
  final CreditCardSummary card;
  final List<AccountSummary> accounts;
  final List<CategorySummary> categories;

  @override
  State<CreditCardDetailPage> createState() => _CreditCardDetailPageState();
}

class _CreditCardDetailPageState extends State<CreditCardDetailPage> {
  List<CreditCardInvoiceSummary> _invoices = const [];
  bool _loading = true;
  String? _error;

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final invoices = await widget.repository.creditCardInvoices(
        tenantId: widget.tenantId,
        cardId: widget.card.id,
      );
      if (mounted) setState(() => _invoices = invoices);
    } catch (_) {
      if (mounted) _error = 'Não foi possível carregar as faturas.';
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _purchase() async {
    final categories = widget.categories
        .where((item) => !item.archived && item.flow == 'Expense')
        .toList(growable: false);
    if (categories.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Cadastre uma categoria de despesa antes.'),
        ),
      );
      return;
    }
    final input = await _showPurchaseForm(context, categories);
    if (input == null || !mounted) return;
    try {
      final state = await widget.repository.createCreditCardPurchase(
        tenantId: widget.tenantId,
        card: widget.card,
        category: input.category,
        amount: input.amount,
        description: input.description,
        installmentCount: input.installmentCount,
      );
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            state == FinanceWriteState.pending
                ? 'Compra pendente de sincronização.'
                : 'Compra registrada. Atualize as faturas.',
          ),
        ),
      );
      await _load();
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Não foi possível registrar a compra.')),
        );
      }
    }
  }

  Future<void> _pay(CreditCardInvoiceSummary invoice) async {
    final accounts = widget.accounts
        .where((item) => !item.archived && item.currency == invoice.currency)
        .toList(growable: false);
    if (accounts.isEmpty || invoice.openAmount <= 0) return;
    final input = await _showPaymentForm(context, invoice, accounts);
    if (input == null || !mounted) return;
    try {
      final state = await widget.repository.payCreditCardInvoice(
        tenantId: widget.tenantId,
        invoice: invoice,
        account: input.account,
        amount: input.amount,
      );
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            state == FinanceWriteState.pending
                ? 'Pagamento pendente de sincronização.'
                : 'Pagamento registrado. Atualize os dados.',
          ),
        ),
      );
      await _load();
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Não foi possível registrar o pagamento.'),
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: Text(widget.card.name)),
    floatingActionButton: FloatingActionButton.extended(
      onPressed: widget.card.archived ? null : _purchase,
      icon: const Icon(Icons.shopping_bag_outlined),
      label: const Text('Registrar compra'),
    ),
    body: _loading
        ? const Center(child: CircularProgressIndicator())
        : _error != null
        ? _RetryState(message: _error!, onRetry: _load)
        : ListView(
            padding: const EdgeInsets.all(16),
            children: [
              _CreditCardTile(card: widget.card),
              const SizedBox(height: 20),
              Text('Faturas', style: Theme.of(context).textTheme.titleLarge),
              const SizedBox(height: 8),
              if (_invoices.isEmpty)
                const Text('Nenhuma fatura disponível ainda.'),
              for (final invoice in _invoices)
                Card(
                  child: ListTile(
                    leading: const Icon(Icons.receipt_long_outlined),
                    title: Text(_month(invoice.competence)),
                    subtitle: Text(
                      'Fecha ${_date(invoice.closingDate)} · vence ${_date(invoice.dueDate)} · ${invoice.status}',
                    ),
                    trailing: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(_money(invoice.openAmount, invoice.currency)),
                        TextButton(
                          onPressed: invoice.openAmount > 0
                              ? () => _pay(invoice)
                              : null,
                          child: const Text('Pagar'),
                        ),
                      ],
                    ),
                  ),
                ),
            ],
          ),
  );
}

class _CreditCardTile extends StatelessWidget {
  const _CreditCardTile({required this.card, this.onTap});
  final CreditCardSummary card;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) => Card(
    color: const Color(0xFF101B39),
    child: InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.credit_card, color: Colors.white),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    card.name,
                    style: const TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
                if (card.archived)
                  const Text(
                    'ARQUIVADO',
                    style: TextStyle(color: Colors.white70),
                  ),
              ],
            ),
            const SizedBox(height: 18),
            Text(
              'Disponível',
              style: TextStyle(color: Colors.white.withValues(alpha: .72)),
            ),
            Text(
              _money(card.availableLimit, card.currency),
              style: const TextStyle(
                color: Colors.white,
                fontSize: 25,
                fontWeight: FontWeight.w800,
              ),
            ),
            const SizedBox(height: 8),
            Text(
            '${card.institution}${card.brand == null ? '' : ' · ${card.brand}'}\nUsado ${_money(card.outstanding, card.currency)} de ${_money(card.limit, card.currency)} · fecha dia ${card.closingDay}',
              style: TextStyle(color: Colors.white.withValues(alpha: .8)),
            ),
          ],
        ),
      ),
    ),
  );
}

class _RetryState extends StatelessWidget {
  const _RetryState({
    required this.message,
    required this.onRetry,
    this.detail,
  });
  final String message;
  final String? detail;
  final Future<void> Function() onRetry;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.credit_card_off_outlined, size: 44),
          const SizedBox(height: 12),
          Text(message, textAlign: TextAlign.center),
          if (detail != null) ...[
            const SizedBox(height: 8),
            Text(detail!, textAlign: TextAlign.center),
          ],
          const SizedBox(height: 16),
          OutlinedButton(onPressed: onRetry, child: const Text('Atualizar')),
        ],
      ),
    ),
  );
}

class _CardInput {
  const _CardInput(this.name, this.limit, this.closingDay, this.dueDay, this.institution, this.brand, this.preferredPaymentAccountId, this.accentColor);
  final String name;
  final num limit;
  final int closingDay;
  final int dueDay;
  final String institution;
  final String? brand;
  final String? preferredPaymentAccountId;
  final String? accentColor;
}

class _PurchaseInput {
  const _PurchaseInput(
    this.category,
    this.amount,
    this.description,
    this.installmentCount,
  );
  final CategorySummary category;
  final num amount;
  final String description;
  final int installmentCount;
}

class _PaymentInput {
  const _PaymentInput(this.account, this.amount);
  final AccountSummary account;
  final num amount;
}

Future<_CardInput?> _showCardForm(BuildContext context) async {
  final name = TextEditingController();
  final institution = TextEditingController();
  final brand = TextEditingController();
  final limit = TextEditingController();
  final closing = TextEditingController(text: '25');
  final due = TextEditingController(text: '5');
  final result = await showDialog<_CardInput>(
    context: context,
    builder: (context) => AlertDialog(
      title: const Text('Novo cartão'),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: name,
              decoration: const InputDecoration(labelText: 'Nome do cartão'),
            ),
    TextField(controller: institution, decoration: const InputDecoration(labelText: 'Instituição financeira')),
    TextField(controller: brand, decoration: const InputDecoration(labelText: 'Bandeira (opcional)')),
            TextField(
              controller: limit,
              keyboardType: const TextInputType.numberWithOptions(
                decimal: true,
              ),
              decoration: const InputDecoration(
                labelText: 'Limite total (R\$)',
              ),
            ),
            TextField(
              controller: closing,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(labelText: 'Dia de fechamento'),
            ),
            TextField(
              controller: due,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(labelText: 'Dia de vencimento'),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Cancelar'),
        ),
        FilledButton(
          onPressed: () {
            final value = _number(limit.text);
            final close = int.tryParse(closing.text);
            final dueDay = int.tryParse(due.text);
            if (name.text.trim().isEmpty || institution.text.trim().isEmpty ||
                value == null ||
                value <= 0 ||
                close == null ||
                dueDay == null ||
                close < 1 ||
                close > 31 ||
                dueDay < 1 ||
                dueDay > 31) {
              return;
            }
            Navigator.pop(
              context,
              _CardInput(name.text.trim(), value, close, dueDay, institution.text.trim(), brand.text.trim().isEmpty ? null : brand.text.trim(), null, null),
            );
          },
          child: const Text('Salvar'),
        ),
      ],
    ),
  );
  name.dispose();
  institution.dispose();
  brand.dispose();
  limit.dispose();
  closing.dispose();
  due.dispose();
  return result;
}

Future<_PurchaseInput?> _showPurchaseForm(
  BuildContext context,
  List<CategorySummary> categories,
) async {
  final amount = TextEditingController();
  final description = TextEditingController();
  final installments = TextEditingController(text: '1');
  CategorySummary selected = categories.first;
  final result = await showDialog<_PurchaseInput>(
    context: context,
    builder: (context) => StatefulBuilder(
      builder: (context, setState) => AlertDialog(
        title: const Text('Compra no cartão'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              DropdownButtonFormField<CategorySummary>(
                initialValue: selected,
                items: categories
                    .map(
                      (item) =>
                          DropdownMenuItem(value: item, child: Text(item.name)),
                    )
                    .toList(),
                onChanged: (item) {
                  if (item != null) {
                    setState(() => selected = item);
                  }
                },
                decoration: const InputDecoration(labelText: 'Categoria'),
              ),
              TextField(
                controller: amount,
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
                decoration: const InputDecoration(labelText: 'Valor (R\$)'),
              ),
              TextField(
                controller: installments,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(
                  labelText: 'Parcelas (1 a 360)',
                ),
              ),
              TextField(
                controller: description,
                maxLength: 500,
                decoration: const InputDecoration(labelText: 'Descrição'),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () {
              final value = _number(amount.text);
              final count = int.tryParse(installments.text);
              if (value == null ||
                  value <= 0 ||
                  count == null ||
                  count < 1 ||
                  count > 360 ||
                  description.text.trim().isEmpty) {
                return;
              }
              Navigator.pop(
                context,
                _PurchaseInput(selected, value, description.text.trim(), count),
              );
            },
            child: const Text('Registrar'),
          ),
        ],
      ),
    ),
  );
  amount.dispose();
  description.dispose();
  installments.dispose();
  return result;
}

Future<_PaymentInput?> _showPaymentForm(
  BuildContext context,
  CreditCardInvoiceSummary invoice,
  List<AccountSummary> accounts,
) async {
  final amount = TextEditingController(
    text: invoice.openAmount.toStringAsFixed(2),
  );
  AccountSummary selected = accounts.first;
  final result = await showDialog<_PaymentInput>(
    context: context,
    builder: (context) => StatefulBuilder(
      builder: (context, setState) => AlertDialog(
        title: const Text('Pagar fatura'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            DropdownButtonFormField<AccountSummary>(
              initialValue: selected,
              items: accounts
                  .map(
                    (item) =>
                        DropdownMenuItem(value: item, child: Text(item.name)),
                  )
                  .toList(),
              onChanged: (item) {
                if (item != null) {
                  setState(() => selected = item);
                }
              },
              decoration: const InputDecoration(
                labelText: 'Conta de pagamento',
              ),
            ),
            TextField(
              controller: amount,
              keyboardType: const TextInputType.numberWithOptions(
                decimal: true,
              ),
              decoration: const InputDecoration(labelText: 'Valor (R\$)'),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () {
              final value = _number(amount.text);
              if (value == null || value <= 0 || value > invoice.openAmount) {
                return;
              }
              Navigator.pop(context, _PaymentInput(selected, value));
            },
            child: const Text('Pagar'),
          ),
        ],
      ),
    ),
  );
  amount.dispose();
  return result;
}

num? _number(String value) {
  var normalized = value.trim();
  if (normalized.contains(',') && normalized.contains('.')) {
    normalized = normalized.replaceAll('.', '').replaceAll(',', '.');
  } else {
    normalized = normalized.replaceAll(',', '.');
  }
  return num.tryParse(normalized);
}

String _money(num value, String currency) =>
    '$currency ${value.toStringAsFixed(2).replaceAll('.', ',')}';
String _date(DateTime date) =>
    '${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}/${date.year}';
String _month(DateTime date) =>
    '${const ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez'][date.month - 1]}/${date.year}';
