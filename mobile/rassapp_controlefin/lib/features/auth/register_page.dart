import 'package:flutter/material.dart';

import '../../core/network/api_client.dart';
import 'auth_controller.dart';

class RegisterPage extends StatefulWidget {
  const RegisterPage({required this.authController, super.key});
  final AuthController authController;
  @override
  State<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends State<RegisterPage> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();
  final _confirmation = TextEditingController();
  bool _waiting = false;
  String? _error;

  @override
  void dispose() {
    _name.dispose();
    _email.dispose();
    _password.dispose();
    _confirmation.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _waiting = true;
      _error = null;
    });
    try {
      await widget.authController.signUp(
        _name.text.trim(),
        _email.text.trim(),
        _password.text,
      );
      if (mounted) Navigator.of(context).pop();
    } on ApiException catch (error) {
      setState(
        () => _error = error.failure == ApiFailure.validation
            ? 'Dados inválidos. Use nome, e-mail válido e senha com 8 a 128 caracteres.'
            : error.failure == ApiFailure.conflict
            ? 'Já existe um cadastro para este e-mail. Entre com sua senha.'
            : error.message,
      );
    } finally {
      if (mounted) setState(() => _waiting = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Criar cadastro')),
    body: SafeArea(
      child: ListView(
        padding: const EdgeInsets.all(24),
        children: [
          Text(
            'Comece a organizar sua vida financeira.',
            style: Theme.of(
              context,
            ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 24),
          Form(
            key: _formKey,
            child: Column(
              children: [
                TextFormField(
                  controller: _name,
                  textCapitalization: TextCapitalization.words,
                  decoration: const InputDecoration(
                    labelText: 'Nome',
                    prefixIcon: Icon(Icons.person_outline),
                  ),
                  validator: (value) => value == null || value.trim().isEmpty
                      ? 'Informe seu nome.'
                      : null,
                ),
                const SizedBox(height: 14),
                TextFormField(
                  controller: _email,
                  keyboardType: TextInputType.emailAddress,
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
                  obscureText: true,
                  decoration: const InputDecoration(
                    labelText: 'Senha',
                    helperText: 'De 8 a 128 caracteres',
                    prefixIcon: Icon(Icons.lock_outline),
                  ),
                  validator: (value) =>
                      value != null && value.length >= 8 && value.length <= 128
                      ? null
                      : 'Use uma senha de 8 a 128 caracteres.',
                ),
                const SizedBox(height: 14),
                TextFormField(
                  controller: _confirmation,
                  obscureText: true,
                  onFieldSubmitted: (_) => _submit(),
                  decoration: const InputDecoration(
                    labelText: 'Confirmar senha',
                    prefixIcon: Icon(Icons.lock_reset_outlined),
                  ),
                  validator: (value) => value == _password.text
                      ? null
                      : 'As senhas não conferem.',
                ),
              ],
            ),
          ),
          if (_error != null)
            Padding(
              padding: const EdgeInsets.only(top: 16),
              child: Text(
                _error!,
                style: const TextStyle(
                  color: Color(0xFFD92D20),
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          const SizedBox(height: 24),
          FilledButton(
            onPressed: _waiting ? null : _submit,
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: 14),
              child: Text(_waiting ? 'Criando...' : 'Criar cadastro'),
            ),
          ),
        ],
      ),
    ),
  );
}
