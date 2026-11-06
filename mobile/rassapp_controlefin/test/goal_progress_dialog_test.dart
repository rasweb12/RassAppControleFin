import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rassapp_controlefin/features/finance/goal_progress_dialog.dart';

void main() {
  testWidgets(
    'focused amount dialog closes safely and returns Brazilian decimal',
    (tester) async {
      num? result;
      await tester.pumpWidget(
        MaterialApp(
          home: Builder(
            builder: (context) => Scaffold(
              body: TextButton(
                onPressed: () async {
                  result = await showDialog<num>(
                    context: context,
                    builder: (_) =>
                        const GoalProgressDialog(goalName: 'Sintética'),
                  );
                },
                child: const Text('Abrir'),
              ),
            ),
          ),
        ),
      );
      await tester.tap(find.text('Abrir'));
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField), '40,50');
      await tester.tap(find.text('Adicionar'));
      await tester.pumpAndSettle();
      expect(result, 40.5);
      expect(tester.takeException(), isNull);
      expect(find.byType(GoalProgressDialog), findsNothing);
    },
  );
}
