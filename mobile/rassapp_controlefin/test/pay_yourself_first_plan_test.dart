import 'package:flutter_test/flutter_test.dart';
import 'package:rassapp_controlefin/features/finance/finance_repository.dart';

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
}
