import 'package:flutter/widgets.dart';

import 'app/rass_app.dart';
import 'core/network/api_client.dart';
import 'core/security/session_store.dart';
import 'features/auth/auth_controller.dart';

void main() {
  final authController = AuthController(ApiClient(), SessionStore());
  runApp(RassApp(authController: authController));
}
