import 'package:flutter_test/flutter_test.dart';
import 'package:rassapp_controlefin/features/finance/finance_repository.dart';
import 'package:rassapp_controlefin/features/finance/pay_yourself_first_page.dart';

void main() {
  test('parses the pay-yourself-first API contract as a civil date', () {
    final plan = PayYourselfFirstPlanSummary.fromJson({
      'id': 'plan-1',
      'currentRate': 0.2,
      'goalRate': 0.3,
      'effectiveFrom': '2026-11-03',
      'version': 2,
    });

    expect(plan.id, 'plan-1');
    expect(plan.currentRate, 0.2);
    expect(plan.goalRate, 0.3);
    expect(plan.effectiveFrom, DateTime(2026, 11, 3));
    expect(plan.version, 2);
  });

  test('calculates a non-mutating monthly suggestion and protects obligations', () {
    final projection = PayYourselfFirstProjection.calculate(
      expectedIncome: 5000,
      receivedIncome: 4500,
      essentialObligations: 3800,
      rate: .2,
    );

    expect(projection.incomeBasis, 4500);
    expect(projection.suggestedReserve, 900);
    expect(projection.balanceAfterObligations, -200);
    expect(projection.isAffordable, isFalse);
    expect(projection.distribution.values.reduce((a, b) => a + b), 900);
  });
}
