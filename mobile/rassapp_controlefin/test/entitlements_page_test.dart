import 'package:flutter_test/flutter_test.dart';
import 'package:rassapp_controlefin/features/monetization/entitlements_page.dart';

void main() {
  group('EntitlementsSnapshot', () {
    test('interprets a valid server response', () {
      final snapshot = EntitlementsSnapshot.fromJson({
        'plan': 'Free',
        'entitlements': [
          {'feature': 'Dashboard', 'enabled': true, 'limit': null},
          {'feature': 'Reports', 'enabled': false, 'limit': 0},
        ],
      });

      expect(snapshot.plan, 'Free');
      expect(snapshot.entitlements, hasLength(2));
      expect(snapshot.entitlements.first.enabled, isTrue);
      expect(snapshot.entitlements.last.limit, 0);
    });

    test('rejects an invalid response contract', () {
      expect(
        () => EntitlementsSnapshot.fromJson({'plan': 'Free'}),
        throwsA(isA<Exception>()),
      );
    });
  });
}
