import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rassapp_controlefin/features/finance/expense_reduction_goal_dialog.dart';

void main() {
  testWidgets(
    'cancel after entering reference closes the dialog without a draft',
    (tester) async {
      var submissions = 0;
      await tester.pumpWidget(
        MaterialApp(
          home: Builder(
            builder: (context) => Scaffold(
              body: TextButton(
                onPressed: () async {
                  final value = await showDialog<(String?, num, num)>(
                    context: context,
                    builder: (_) =>
                        const ExpenseReductionGoalDialog(categories: []),
                  );
                  if (value != null) submissions++;
                },
                child: const Text('Redução de Despesas'),
              ),
            ),
          ),
        ),
      );
      await tester.tap(find.text('Redução de Despesas'));
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField).first, '1000');
      tester.testTextInput.hide();
      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
      expect(find.byType(ExpenseReductionGoalDialog), findsNothing);
      expect(find.text('Redução de Despesas'), findsOneWidget);
      expect(submissions, 0);
    },
  );
}
