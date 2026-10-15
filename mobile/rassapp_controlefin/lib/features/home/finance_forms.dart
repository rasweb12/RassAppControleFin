import 'package:flutter/material.dart';

import '../finance/finance_repository.dart';

enum FinanceAction { account, category, income, expense, transfer }

class AccountInput {
  const AccountInput({required this.name, required this.openingBalance});
  final String name;
  final num openingBalance;
}

class CategoryInput {
  const CategoryInput({required this.name, required this.flow});
  final String name;
  final String flow;
}

class TransactionInput {
  const TransactionInput({
    required this.account,
    required this.category,
    required this.amount,
    required this.description,
  });
  final AccountSummary account;
  final CategorySummary category;
  final num amount;
  final String description;
}

class TransferInput {
  const TransferInput({
    required this.source,
    required this.destination,
    required this.amount,
    required this.description,
  });
  final AccountSummary source;
  final AccountSummary destination;
  final num amount;
  final String description;
}

Future<FinanceAction?> showFinanceActionSheet(BuildContext context) =>
    showModalBottomSheet<FinanceAction>(
      context: context,
      builder: (context) => SafeArea(
        child: ListView(
          shrinkWrap: true,
          children: [
            const ListTile(title: Text('Novo lançamento')),
            ListTile(
              leading: const Icon(Icons.add_card_outlined),
              title: const Text('Nova conta'),
              onTap: () => Navigator.pop(context, FinanceAction.account),
            ),
            ListTile(
              leading: const Icon(Icons.sell_outlined),
              title: const Text('Nova categoria'),
              onTap: () => Navigator.pop(context, FinanceAction.category),
            ),
            ListTile(
              leading: const Icon(Icons.south_west_outlined),
              title: const Text('Registrar receita'),
              onTap: () => Navigator.pop(context, FinanceAction.income),
            ),
            ListTile(
              leading: const Icon(Icons.north_east_outlined),
              title: const Text('Registrar despesa'),
              onTap: () => Navigator.pop(context, FinanceAction.expense),
            ),
            ListTile(
              leading: const Icon(Icons.swap_horiz_outlined),
              title: const Text('Transferir entre contas'),
              onTap: () => Navigator.pop(context, FinanceAction.transfer),
            ),
          ],
        ),
      ),
    );

Future<AccountInput?> showAccountForm(BuildContext context) =>
    showModalBottomSheet<AccountInput>(
      context: context,
      isScrollControlled: true,
      builder: (_) => const _AccountForm(),
    );

Future<CategoryInput?> showCategoryForm(BuildContext context) =>
    showModalBottomSheet<CategoryInput>(
      context: context,
      isScrollControlled: true,
      builder: (_) => const _CategoryForm(),
    );

Future<TransactionInput?> showTransactionForm(
  BuildContext context, {
  required String flow,
  required List<AccountSummary> accounts,
  required List<CategorySummary> categories,
}) => showModalBottomSheet<TransactionInput>(
  context: context,
  isScrollControlled: true,
  builder: (_) =>
      _TransactionForm(flow: flow, accounts: accounts, categories: categories),
);

Future<TransferInput?> showTransferForm(
  BuildContext context,
  List<AccountSummary> accounts,
) => showModalBottomSheet<TransferInput>(
  context: context,
  isScrollControlled: true,
  builder: (_) => _TransferForm(accounts: accounts),
);

class _AccountForm extends StatefulWidget {
  const _AccountForm();
  @override
  State<_AccountForm> createState() => _AccountFormState();
}

class _AccountFormState extends State<_AccountForm> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _balance = TextEditingController(text: '0');

  @override
  void dispose() {
    _name.dispose();
    _balance.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => _Sheet(
    title: 'Nova conta',
    formKey: _formKey,
    fields: [
      TextFormField(
        controller: _name,
        autofocus: true,
        decoration: const InputDecoration(labelText: 'Nome da conta'),
        validator: _required,
      ),
      const SizedBox(height: 12),
      TextFormField(
        controller: _balance,
        keyboardType: const TextInputType.numberWithOptions(decimal: true),
        decoration: const InputDecoration(labelText: 'Saldo inicial (R\$)'),
        validator: (value) {
          final amount = _number(value);
          return amount == null || amount < 0
              ? 'Informe um valor igual ou maior que zero.'
              : null;
        },
      ),
    ],
    submit: 'Salvar conta',
    onSubmit: () {
      if (!_formKey.currentState!.validate()) return;
      Navigator.pop(
        context,
        AccountInput(
          name: _name.text.trim(),
          openingBalance: _number(_balance.text)!,
        ),
      );
    },
  );
}

class _CategoryForm extends StatefulWidget {
  const _CategoryForm();
  @override
  State<_CategoryForm> createState() => _CategoryFormState();
}

class _CategoryFormState extends State<_CategoryForm> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  String _flow = 'Expense';

  @override
  void dispose() {
    _name.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => _Sheet(
    title: 'Nova categoria',
    formKey: _formKey,
    fields: [
      TextFormField(
        controller: _name,
        autofocus: true,
        decoration: const InputDecoration(labelText: 'Nome da categoria'),
        validator: _required,
      ),
      const SizedBox(height: 12),
      DropdownButtonFormField<String>(
        initialValue: _flow,
        decoration: const InputDecoration(labelText: 'Tipo'),
        items: const [
          DropdownMenuItem(value: 'Expense', child: Text('Despesa')),
          DropdownMenuItem(value: 'Income', child: Text('Receita')),
        ],
        onChanged: (value) => setState(() => _flow = value ?? 'Expense'),
      ),
    ],
    submit: 'Salvar categoria',
    onSubmit: () {
      if (!_formKey.currentState!.validate()) return;
      Navigator.pop(
        context,
        CategoryInput(name: _name.text.trim(), flow: _flow),
      );
    },
  );
}

class _TransactionForm extends StatefulWidget {
  const _TransactionForm({
    required this.flow,
    required this.accounts,
    required this.categories,
  });
  final String flow;
  final List<AccountSummary> accounts;
  final List<CategorySummary> categories;
  @override
  State<_TransactionForm> createState() => _TransactionFormState();
}

class _TransactionFormState extends State<_TransactionForm> {
  final _formKey = GlobalKey<FormState>();
  final _amount = TextEditingController();
  final _description = TextEditingController();
  String? _accountId;
  String? _categoryId;

  @override
  void dispose() {
    _amount.dispose();
    _description.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => _Sheet(
    title: widget.flow == 'Income' ? 'Registrar receita' : 'Registrar despesa',
    formKey: _formKey,
    fields: [
      DropdownButtonFormField<String>(
        initialValue: _accountId,
        decoration: const InputDecoration(labelText: 'Conta'),
        items: widget.accounts
            .map(
              (item) =>
                  DropdownMenuItem(value: item.id, child: Text(item.name)),
            )
            .toList(),
        onChanged: (value) => setState(() => _accountId = value),
        validator: (value) => value == null ? 'Selecione uma conta.' : null,
      ),
      const SizedBox(height: 12),
      DropdownButtonFormField<String>(
        initialValue: _categoryId,
        decoration: const InputDecoration(labelText: 'Categoria'),
        items: widget.categories
            .map(
              (item) =>
                  DropdownMenuItem(value: item.id, child: Text(item.name)),
            )
            .toList(),
        onChanged: (value) => setState(() => _categoryId = value),
        validator: (value) => value == null ? 'Selecione uma categoria.' : null,
      ),
      const SizedBox(height: 12),
      TextFormField(
        controller: _amount,
        keyboardType: const TextInputType.numberWithOptions(decimal: true),
        decoration: const InputDecoration(labelText: 'Valor (R\$)'),
        validator: _positive,
      ),
      const SizedBox(height: 12),
      TextFormField(
        controller: _description,
        maxLength: 500,
        decoration: const InputDecoration(labelText: 'Descrição (opcional)'),
      ),
    ],
    submit: 'Registrar',
    onSubmit: () {
      if (!_formKey.currentState!.validate()) return;
      final account = widget.accounts.firstWhere(
        (item) => item.id == _accountId,
      );
      final category = widget.categories.firstWhere(
        (item) => item.id == _categoryId,
      );
      Navigator.pop(
        context,
        TransactionInput(
          account: account,
          category: category,
          amount: _number(_amount.text)!,
          description: _description.text.trim(),
        ),
      );
    },
  );
}

class _TransferForm extends StatefulWidget {
  const _TransferForm({required this.accounts});
  final List<AccountSummary> accounts;
  @override
  State<_TransferForm> createState() => _TransferFormState();
}

class _TransferFormState extends State<_TransferForm> {
  final _formKey = GlobalKey<FormState>();
  final _amount = TextEditingController();
  final _description = TextEditingController();
  String? _sourceId;
  String? _destinationId;

  @override
  void dispose() {
    _amount.dispose();
    _description.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => _Sheet(
    title: 'Transferir entre contas',
    formKey: _formKey,
    fields: [
      DropdownButtonFormField<String>(
        initialValue: _sourceId,
        decoration: const InputDecoration(labelText: 'Conta de origem'),
        items: widget.accounts
            .map(
              (item) =>
                  DropdownMenuItem(value: item.id, child: Text(item.name)),
            )
            .toList(),
        onChanged: (value) => setState(() => _sourceId = value),
        validator: (value) =>
            value == null ? 'Selecione a conta de origem.' : null,
      ),
      const SizedBox(height: 12),
      DropdownButtonFormField<String>(
        initialValue: _destinationId,
        decoration: const InputDecoration(labelText: 'Conta de destino'),
        items: widget.accounts
            .map(
              (item) =>
                  DropdownMenuItem(value: item.id, child: Text(item.name)),
            )
            .toList(),
        onChanged: (value) => setState(() => _destinationId = value),
        validator: (value) {
          if (value == null) return 'Selecione a conta de destino.';
          return value == _sourceId ? 'Escolha contas diferentes.' : null;
        },
      ),
      const SizedBox(height: 12),
      TextFormField(
        controller: _amount,
        keyboardType: const TextInputType.numberWithOptions(decimal: true),
        decoration: const InputDecoration(labelText: 'Valor (R\$)'),
        validator: _positive,
      ),
      const SizedBox(height: 12),
      TextFormField(
        controller: _description,
        maxLength: 500,
        decoration: const InputDecoration(labelText: 'Descrição (opcional)'),
      ),
    ],
    submit: 'Transferir',
    onSubmit: () {
      if (!_formKey.currentState!.validate()) return;
      final source = widget.accounts.firstWhere((item) => item.id == _sourceId);
      final destination = widget.accounts.firstWhere(
        (item) => item.id == _destinationId,
      );
      if (source.currency != destination.currency) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('As contas devem usar a mesma moeda.')),
        );
        return;
      }
      Navigator.pop(
        context,
        TransferInput(
          source: source,
          destination: destination,
          amount: _number(_amount.text)!,
          description: _description.text.trim(),
        ),
      );
    },
  );
}

class _Sheet extends StatelessWidget {
  const _Sheet({
    required this.title,
    required this.formKey,
    required this.fields,
    required this.submit,
    required this.onSubmit,
  });
  final String title;
  final GlobalKey<FormState> formKey;
  final List<Widget> fields;
  final String submit;
  final VoidCallback onSubmit;
  @override
  Widget build(BuildContext context) => Padding(
    padding: EdgeInsets.fromLTRB(
      24,
      24,
      24,
      24 + MediaQuery.viewInsetsOf(context).bottom,
    ),
    child: SingleChildScrollView(
      child: Form(
        key: formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              title,
              style: Theme.of(
                context,
              ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
            ),
            const SizedBox(height: 18),
            ...fields,
            const SizedBox(height: 20),
            FilledButton(onPressed: onSubmit, child: Text(submit)),
          ],
        ),
      ),
    ),
  );
}

String? _required(String? value) =>
    value == null || value.trim().isEmpty ? 'Preencha este campo.' : null;
num? _number(String? value) {
  var normalized = (value ?? '').trim();
  if (normalized.contains(',') && normalized.contains('.')) {
    normalized = normalized.replaceAll('.', '').replaceAll(',', '.');
  } else {
    normalized = normalized.replaceAll(',', '.');
  }
  return num.tryParse(normalized);
}

String? _positive(String? value) =>
    _number(value) == null || _number(value)! <= 0
    ? 'Informe um valor maior que zero.'
    : null;
