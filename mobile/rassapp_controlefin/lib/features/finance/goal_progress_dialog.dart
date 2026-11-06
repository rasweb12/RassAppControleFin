import 'package:flutter/material.dart';

class GoalProgressDialog extends StatefulWidget {
  const GoalProgressDialog({required this.goalName, super.key});
  final String goalName;

  @override
  State<GoalProgressDialog> createState() => _GoalProgressDialogState();
}

class _GoalProgressDialogState extends State<GoalProgressDialog> {
  final _amount = TextEditingController();

  @override
  void dispose() {
    _amount.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AlertDialog(
    title: Text('Adicionar à meta ${widget.goalName}'),
    content: TextField(
      controller: _amount,
      autofocus: true,
      keyboardType: const TextInputType.numberWithOptions(decimal: true),
      decoration: const InputDecoration(labelText: 'Valor (R\$)'),
    ),
    actions: [
      TextButton(
        onPressed: () => Navigator.of(context).pop(),
        child: const Text('Cancelar'),
      ),
      FilledButton(
        onPressed: () {
          final value = num.tryParse(_amount.text.trim().replaceAll(',', '.'));
          if (value == null || !value.isFinite || value <= 0) return;
          Navigator.of(context).pop(value);
        },
        child: const Text('Adicionar'),
      ),
    ],
  );
}
