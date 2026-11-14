import 'package:flutter/material.dart';

import 'finance_repository.dart';

class ExpenseReductionGoalDialog extends StatefulWidget {
  const ExpenseReductionGoalDialog({required this.categories, super.key});

  final List<CategorySummary> categories;

  @override
  State<ExpenseReductionGoalDialog> createState() =>
      _ExpenseReductionGoalDialogState();
}

class _ExpenseReductionGoalDialogState
    extends State<ExpenseReductionGoalDialog> {
  final _reference = TextEditingController();
  final _target = TextEditingController();
  String? _categoryId;

  @override
  void dispose() {
    _reference.dispose();
    _target.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AlertDialog(
    title: const Text('Meta de redução mensal'),
    scrollable: true,
    content: Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        DropdownButtonFormField<String?>(
          initialValue: _categoryId,
          decoration: const InputDecoration(labelText: 'Categoria (opcional)'),
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
          onChanged: (value) => _categoryId = value,
        ),
        const SizedBox(height: 12),
        TextField(
          controller: _reference,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          decoration: const InputDecoration(
            labelText: 'Gasto de referência (R\$)',
          ),
        ),
        TextField(
          controller: _target,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          decoration: const InputDecoration(labelText: 'Limite desejado (R\$)'),
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
          final from = num.tryParse(_reference.text.replaceAll(',', '.'));
          final to = num.tryParse(_target.text.replaceAll(',', '.'));
          if (from == null ||
              to == null ||
              !from.isFinite ||
              !to.isFinite ||
              from <= 0 ||
              to < 0 ||
              to >= from) {
            return;
          }
          Navigator.pop(context, (_categoryId, from, to));
        },
        child: const Text('Criar'),
      ),
    ],
  );
}
