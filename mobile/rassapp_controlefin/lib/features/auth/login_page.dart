import 'package:flutter/material.dart';

import '../../core/network/api_client.dart';
import '../../core/theme/app_theme.dart';
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
  bool _remember = true;
  String? _error;

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _signIn() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _waiting = true;
      _error = null;
    });
    try {
      await widget.authController.signIn(
        _email.text.trim(),
        _password.text,
        rememberSession: _remember,
      );
    } on ApiException catch (error) {
      if (!mounted) return;
      setState(
        () => _error = switch (error.failure) {
          ApiFailure.unauthorized => 'E-mail ou senha incorretos.',
          ApiFailure.unavailable =>
            'A autenticação está temporariamente indisponível.',
          ApiFailure.validation => 'Revise os campos informados.',
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
            padding: const EdgeInsets.fromLTRB(28, 36, 28, 24),
            children: [
              Center(
                child: Image.asset(
                  'assets/branding/rass_logo.png',
                  width: 112,
                  height: 112,
                  semanticLabel: 'Rass Controle Financeiro',
                ),
              ),
              const SizedBox(height: 14),
              Text(
                'RASS',
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                  fontWeight: FontWeight.w900,
                  color: AppTheme.navy,
                ),
              ),
              const SizedBox(height: 8),
              const Text(
                'Controle financeiro inteligente.',
                textAlign: TextAlign.center,
                style: TextStyle(color: AppTheme.muted, fontSize: 18),
              ),
              const SizedBox(height: 42),
              Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const _Label('E-MAIL'),
                    const SizedBox(height: 8),
                    TextFormField(
                      controller: _email,
                      keyboardType: TextInputType.emailAddress,
                      autocorrect: false,
                      textInputAction: TextInputAction.next,
                      decoration: const InputDecoration(
                        hintText: 'seu@email.com',
                        prefixIcon: Icon(Icons.mail_outline),
                      ),
                      validator: (value) =>
                          value != null &&
                              RegExp(r'^\S+@\S+\.\S+$').hasMatch(value.trim())
                          ? null
                          : 'Informe um e-mail válido.',
                    ),
                    const SizedBox(height: 22),
                    const _Label('SENHA'),
                    const SizedBox(height: 8),
                    TextFormField(
                      controller: _password,
                      obscureText: _obscure,
                      enableSuggestions: false,
                      autocorrect: false,
                      onFieldSubmitted: (_) => _signIn(),
                      decoration: InputDecoration(
                        hintText: '••••••••',
                        prefixIcon: const Icon(Icons.lock_outline),
                        suffixIcon: IconButton(
                          tooltip: _obscure ? 'Mostrar senha' : 'Ocultar senha',
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
              Row(
                children: [
                  Checkbox(
                    value: _remember,
                    onChanged: _waiting
                        ? null
                        : (value) => setState(() => _remember = value ?? false),
                  ),
                  const Expanded(
                    child: Text(
                      'Lembrar de mim',
                      style: TextStyle(fontWeight: FontWeight.w700),
                    ),
                  ),
                  TextButton(
                    onPressed: _waiting ? null : _showRecovery,
                    child: const Text('Esqueci minha senha'),
                  ),
                ],
              ),
              if (_error != null)
                Text(
                  _error!,
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    color: AppTheme.expense,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              const SizedBox(height: 20),
              FilledButton.icon(
                onPressed: _waiting ? null : _signIn,
                icon: const Icon(Icons.arrow_forward),
                label: Text(_waiting ? 'Entrando...' : 'Entrar'),
              ),
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 20),
                child: Row(
                  children: [
                    Expanded(child: Divider()),
                    Padding(
                      padding: EdgeInsets.symmetric(horizontal: 12),
                      child: Text('ou'),
                    ),
                    Expanded(child: Divider()),
                  ],
                ),
              ),
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
              const SizedBox(height: 34),
              const Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(
                    Icons.verified_user_outlined,
                    color: AppTheme.income,
                    size: 19,
                  ),
                  SizedBox(width: 8),
                  Text(
                    'Sessão protegida via Neon Auth',
                    style: TextStyle(color: AppTheme.muted),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    ),
  );

  Future<void> _showRecovery() async {
    final email = TextEditingController(text: _email.text.trim());
    var waiting = false;
    String? error;
    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (sheetContext) => StatefulBuilder(
        builder: (context, setSheetState) => Padding(
          padding: EdgeInsets.fromLTRB(
            28,
            20,
            28,
            MediaQuery.viewInsetsOf(context).bottom + 28,
          ),
          child: SafeArea(
            top: false,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    const CircleAvatar(
                      backgroundColor: Color(0xFFEFF6FF),
                      child: Icon(Icons.key_outlined, color: AppTheme.blue),
                    ),
                    const SizedBox(width: 14),
                    const Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Recuperar senha',
                            style: TextStyle(
                              fontSize: 24,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                          Text(
                            'Etapa de validação e segurança',
                            style: TextStyle(color: AppTheme.muted),
                          ),
                        ],
                      ),
                    ),
                    IconButton(
                      onPressed: waiting
                          ? null
                          : () => Navigator.pop(sheetContext),
                      icon: const Icon(Icons.close),
                    ),
                  ],
                ),
                const SizedBox(height: 24),
                const Text(
                  'Enviaremos um link de recuperação caso este e-mail possua cadastro ativo.',
                  style: TextStyle(fontSize: 17, height: 1.45),
                ),
                const SizedBox(height: 20),
                const _Label('E-MAIL CADASTRADO'),
                const SizedBox(height: 8),
                TextField(
                  controller: email,
                  keyboardType: TextInputType.emailAddress,
                  decoration: const InputDecoration(
                    prefixIcon: Icon(Icons.mail_outline),
                    hintText: 'seu@email.com',
                  ),
                ),
                const SizedBox(height: 14),
                const Text(
                  'Verifique também a caixa de spam após o envio.',
                  style: TextStyle(color: Color(0xFF92400E)),
                ),
                if (error != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 12),
                    child: Text(
                      error!,
                      style: const TextStyle(color: AppTheme.expense),
                    ),
                  ),
                const SizedBox(height: 24),
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton(
                        onPressed: waiting
                            ? null
                            : () => Navigator.pop(sheetContext),
                        child: const Text('Cancelar'),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: FilledButton.icon(
                        onPressed: waiting
                            ? null
                            : () async {
                                final value = email.text.trim();
                                if (!RegExp(
                                  r'^\S+@\S+\.\S+$',
                                ).hasMatch(value)) {
                                  setSheetState(
                                    () => error = 'Informe um e-mail válido.',
                                  );
                                  return;
                                }
                                setSheetState(() {
                                  waiting = true;
                                  error = null;
                                });
                                try {
                                  await widget.authController
                                      .requestPasswordReset(value);
                                  if (sheetContext.mounted) {
                                    Navigator.pop(sheetContext);
                                  }
                                  if (mounted) {
                                    ScaffoldMessenger.of(
                                      this.context,
                                    ).showSnackBar(
                                      const SnackBar(
                                        content: Text(
                                          'Caso exista uma conta para este e-mail, enviaremos um link de recuperação.',
                                        ),
                                      ),
                                    );
                                  }
                                } on ApiException catch (exception) {
                                  if (sheetContext.mounted) {
                                    setSheetState(
                                      () => error = exception.message,
                                    );
                                  }
                                } finally {
                                  if (sheetContext.mounted) {
                                    setSheetState(() => waiting = false);
                                  }
                                }
                              },
                        icon: const Icon(Icons.arrow_forward),
                        label: Text(waiting ? 'Enviando...' : 'Enviar link'),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
    email.dispose();
  }
}

class _Label extends StatelessWidget {
  const _Label(this.value);
  final String value;
  @override
  Widget build(BuildContext context) => Text(
    value,
    style: const TextStyle(
      color: AppTheme.navy,
      fontWeight: FontWeight.w800,
      letterSpacing: 0.4,
    ),
  );
}
