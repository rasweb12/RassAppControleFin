import 'package:flutter/material.dart';

import 'app/rass_app.dart';
import 'core/config/app_config.dart';
import 'core/network/api_client.dart';
import 'core/security/session_store.dart';
import 'features/auth/auth_controller.dart';

void main() {
  if (!AppConfig.isConfigured) {
    runApp(_ConfigurationErrorApp(message: AppConfig.configurationError!));
    return;
  }
  final authController = AuthController(ApiClient(), SessionStore());
  runApp(RassApp(authController: authController));
}

class _ConfigurationErrorApp extends StatelessWidget {
  const _ConfigurationErrorApp({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) => MaterialApp(
    home: Scaffold(
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Text(message, textAlign: TextAlign.center),
        ),
      ),
    ),
  );
}
