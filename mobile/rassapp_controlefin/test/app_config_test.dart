import 'package:flutter_test/flutter_test.dart';
import 'package:rassapp_controlefin/core/config/app_config.dart';

void main() {
  test('unconfigured builds never default to the public Production API', () {
    expect(AppConfig.apiBaseUri.host, 'configuration-required.invalid');
    expect(AppConfig.isConfigured, isFalse);
    expect(AppConfig.configurationError, isNotEmpty);
  });
}
