import 'package:flutter/material.dart';

import '../../core/network/api_client.dart';
import 'auth_controller.dart';
import 'register_page.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({required this.authController, super.key});
  final AuthController authController;
  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _password = TextEditingController();
  bool _waiting = false;
  bool _obscure = true;
  String? _error;

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _waiting = true;
      _error = null;
    });
    try {
      await widget.authController.signIn(_email.text.trim(), _password.text);
    } on ApiException catch (error) {
      setState(
        () => _error = switch (error.failure) {
          ApiFailure.validation => 'Revise os campos informados.',
          ApiFailure.unauthorized => 'E-mail ou senha incorretos.',
          _ => error.message,
        },
      );
    } finally {
      if (mounted) setState(() => _waiting = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    body: SafeArea(
      child: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 440),
          child: ListView(
            padding: const EdgeInsets.all(24),
            children: [
              const SizedBox(height: 44),
              const Icon(
                Icons.account_balance_wallet_rounded,
                size: 58,
                color: Color(0xFF001B3D),
              ),
              const SizedBox(height: 18),
              Text(
                'RASS',
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                  fontWeight: FontWeight.w900,
                ),
              ),
              const SizedBox(height: 6),
              Text(
                'Controle financeiro inteligente.',
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodyLarge,
              ),
              const SizedBox(height: 36),
              Form(
                key: _formKey,
                child: Column(
                  children: [
                    TextFormField(
                      controller: _email,
                      keyboardType: TextInputType.emailAddress,
                      autocorrect: false,
                      decoration: const InputDecoration(
                        labelText: 'E-mail',
                        prefixIcon: Icon(Icons.mail_outline),
                      ),
                      validator: (value) =>
                          value != null &&
                              RegExp(r'^\S+@\S+\.\S+$').hasMatch(value.trim())
                          ? null
                          : 'Informe um e-mail válido.',
                    ),
                    const SizedBox(height: 14),
                    TextFormField(
                      controller: _password,
                      obscureText: _obscure,
                      enableSuggestions: false,
                      autocorrect: false,
                      onFieldSubmitted: (_) => _submit(),
                      decoration: InputDecoration(
                        labelText: 'Senha',
                        prefixIcon: const Icon(Icons.lock_outline),
                        suffixIcon: IconButton(
                          onPressed: () => setState(() => _obscure = !_obscure),
                          icon: Icon(
                            _obscure
                                ? Icons.visibility_outlined
                                : Icons.visibility_off_outlined,
                          ),
                        ),
                      ),
                      validator: (value) => value == null || value.isEmpty
                          ? 'Informe sua senha.'
                          : null,
                    ),
                  ],
                ),
              ),
              if (_error != null)
                Padding(
                  padding: const EdgeInsets.only(top: 16),
                  child: Text(
                    _error!,
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                      color: Color(0xFFD92D20),
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
              const SizedBox(height: 22),
              FilledButton(
                onPressed: _waiting ? null : _submit,
                child: Padding(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  child: Text(_waiting ? 'Entrando...' : 'Entrar'),
                ),
              ),
              const SizedBox(height: 10),
              OutlinedButton.icon(
                onPressed: _waiting
                    ? null
                    : () => Navigator.of(context).push(
                        MaterialPageRoute(
                          builder: (_) => RegisterPage(
                            authController: widget.authController,
                          ),
                        ),
                      ),
                icon: const Icon(Icons.person_add_alt_1_outlined),
                label: const Text('Criar cadastro'),
              ),
              TextButton(
                onPressed: _waiting ? null : () => _showRecovery(context),
                child: const Text('Esqueci minha senha'),
              ),
            ],
          ),
        ),
      ),
    ),
  );

  Future<void> _showRecovery(BuildContext context) async {
    final email = TextEditingController(text: _email.text.trim());
    var waiting = false;
    String? error;
    await showDialog<void>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: const Text('Recuperar senha'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Text(
                'Enviaremos um link caso este e-mail possua cadastro.',
              ),
              const SizedBox(height: 14),
              TextField(
                controller: email,
                keyboardType: TextInputType.emailAddress,
                autocorrect: false,
                decoration: const InputDecoration(labelText: 'E-mail'),
              ),
              if (error != null)
                Padding(
                  padding: const EdgeInsets.only(top: 12),
                  child: Text(
                    error!,
                    style: const TextStyle(color: Color(0xFFD92D20)),
                  ),
                ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: waiting ? null : () => Navigator.pop(dialogContext),
              child: const Text('Cancelar'),
            ),
            FilledButton(
              onPressed: waiting
                  ? null
                  : () async {
                      final value = email.text.trim();
                      if (!RegExp(r'^\S+@\S+\.\S+$').hasMatch(value)) {
                        setDialogState(
                          () => error = 'Informe um e-mail válido.',
                        );
                        return;
                      }
                      setDialogState(() {
                        waiting = true;
                        error = null;
                      });
                      try {
                        await widget.authController.requestPasswordReset(value);
                        if (dialogContext.mounted) Navigator.pop(dialogContext);
                        if (mounted) {
                          ScaffoldMessenger.of(this.context).showSnackBar(
                            const SnackBar(
                              content: Text(
                                'Caso exista uma conta para este e-mail, enviaremos um link de recuperação.',
                              ),
                            ),
                          );
                        }
                      } on ApiException catch (exception) {
                        if (dialogContext.mounted) {
                          setDialogState(() => error = exception.message);
                        }
                      } finally {
                        if (dialogContext.mounted)
                          setDialogState(() => waiting = false);
                      }
                    },
              child: Text(waiting ? 'Enviando...' : 'Enviar link'),
            ),
          ],
        ),
      ),
    );
    email.dispose();
  }
}
