import 'package:flutter/material.dart';

import '../core/theme/app_theme.dart';
import '../features/auth/auth_controller.dart';
import '../features/auth/login_page.dart';
import '../features/home/home_page.dart';

class RassApp extends StatefulWidget {
  const RassApp({required this.authController, super.key});
  final AuthController authController;
  @override
  State<RassApp> createState() => _RassAppState();
}

class _RassAppState extends State<RassApp> {
  @override
  void initState() {
    super.initState();
    widget.authController.restore();
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: widget.authController,
    builder: (context, _) => MaterialApp(
      title: 'RASS Controle Financeiro',
      theme: AppTheme.light(),
      debugShowCheckedModeBanner: false,
      home: widget.authController.isLoading
          ? const Scaffold(body: Center(child: CircularProgressIndicator()))
          : widget.authController.isAuthenticated
          ? HomePage(authController: widget.authController)
          : LoginPage(authController: widget.authController),
    ),
  );
}
