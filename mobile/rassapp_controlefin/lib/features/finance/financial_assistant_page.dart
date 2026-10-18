import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../core/network/api_client.dart';
import 'finance_repository.dart';

class FinancialAssistantPage extends StatefulWidget {
  const FinancialAssistantPage({
    required this.repository,
    required this.tenantId,
    required this.subject,
    super.key,
  });

  final FinanceRepository repository;
  final String tenantId;
  final String subject;

  @override
  State<FinancialAssistantPage> createState() => _FinancialAssistantPageState();
}

class _FinancialAssistantPageState extends State<FinancialAssistantPage> {
  static const _suggestions = <String>[
    'Quanto gastei neste mês?',
    'Onde posso economizar?',
    'Como está meu orçamento?',
    'Como melhorar minha reserva?',
  ];

  final _input = TextEditingController();
  final _scroll = ScrollController();
  final _secureStorage = const FlutterSecureStorage();
  final _messages = <_ChatMessage>[];
  bool _consented = false;
  bool _consentReady = false;
  bool _sending = false;

  @override
  void initState() {
    super.initState();
    _loadConsent();
  }

  @override
  void dispose() {
    _input.dispose();
    _scroll.dispose();
    super.dispose();
  }

  String get _consentKey => 'ai-consent.${widget.subject}.${widget.tenantId}';

  Future<void> _loadConsent() async {
    final value = await _secureStorage.read(key: _consentKey);
    if (mounted) {
      setState(() {
        _consented = value == 'true';
        _consentReady = true;
      });
    }
  }

  Future<void> _acceptConsent() async {
    await _secureStorage.write(key: _consentKey, value: 'true');
    if (mounted) setState(() => _consented = true);
  }

  Future<void> _revokeConsent() async {
    await _secureStorage.delete(key: _consentKey);
    if (mounted) {
      setState(() {
        _consented = false;
        _messages.clear();
      });
    }
  }

  Future<void> _ask([String? suggested]) async {
    final question = (suggested ?? _input.text).trim();
    if (question.isEmpty || _sending || !_consented) return;
    setState(() {
      _messages.add(_ChatMessage.user(question));
      _input.clear();
      _sending = true;
    });
    _scrollToEnd();
    final now = DateTime.now();
    try {
      final answer = await widget.repository.askFinancialAssistant(
        tenantId: widget.tenantId,
        question: question,
        period: '${now.year}-${now.month.toString().padLeft(2, '0')}',
      );
      if (mounted) {
        setState(() => _messages.add(_ChatMessage.assistant(answer)));
      }
    } on ApiException catch (error) {
      if (mounted) {
        setState(
          () => _messages.add(
            _ChatMessage.system(
              error.failure == ApiFailure.offline
                  ? 'O assistente precisa de conexão. Seus dados locais continuam disponíveis no aplicativo.'
                  : error.message,
            ),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _sending = false);
      _scrollToEnd();
    }
  }

  void _scrollToEnd() => WidgetsBinding.instance.addPostFrameCallback((_) {
    if (_scroll.hasClients) {
      _scroll.animateTo(
        _scroll.position.maxScrollExtent,
        duration: const Duration(milliseconds: 180),
        curve: Curves.easeOut,
      );
    }
  });

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('Assistente financeiro'),
      actions: [
        if (_messages.isNotEmpty)
          IconButton(
            tooltip: 'Limpar conversa',
            icon: const Icon(Icons.delete_outline),
            onPressed: _sending ? null : () => setState(_messages.clear),
          ),
        if (_consented)
          IconButton(
            tooltip: 'Revogar consentimento',
            icon: const Icon(Icons.privacy_tip_outlined),
            onPressed: _sending ? null : _revokeConsent,
          ),
      ],
    ),
    body: !_consentReady
        ? const Center(child: CircularProgressIndicator())
        : !_consented
        ? _ConsentPanel(onAccept: _acceptConsent)
        : Column(
            children: [
              Expanded(
                child: _messages.isEmpty
                    ? _WelcomePanel(onSuggestion: _ask)
                    : ListView.builder(
                        controller: _scroll,
                        padding: const EdgeInsets.all(16),
                        itemCount: _messages.length + (_sending ? 1 : 0),
                        itemBuilder: (context, index) {
                          if (index == _messages.length) {
                            return const _ChatBubble(
                              text: 'Analisando seus dados autorizados...',
                              assistant: true,
                              loading: true,
                            );
                          }
                          final message = _messages[index];
                          return _ChatBubble(
                            text: message.text,
                            assistant: message.kind != _ChatKind.user,
                            system: message.kind == _ChatKind.system,
                          );
                        },
                      ),
              ),
              Padding(
                padding: EdgeInsets.fromLTRB(
                  16,
                  8,
                  16,
                  12 + MediaQuery.viewInsetsOf(context).bottom,
                ),
                child: Row(
                  children: [
                    Expanded(
                      child: TextField(
                        controller: _input,
                        minLines: 1,
                        maxLines: 4,
                        textInputAction: TextInputAction.send,
                        onSubmitted: (_) => _ask(),
                        decoration: const InputDecoration(
                          hintText: 'Pergunte sobre suas finanças',
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    IconButton.filled(
                      tooltip: 'Enviar pergunta',
                      onPressed: _sending ? null : _ask,
                      icon: const Icon(Icons.send),
                    ),
                  ],
                ),
              ),
            ],
          ),
  );
}

class _ConsentPanel extends StatelessWidget {
  const _ConsentPanel({required this.onAccept});
  final VoidCallback onAccept;

  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.privacy_tip_outlined, size: 48),
          const SizedBox(height: 16),
          Text(
            'Uso de dados no assistente',
            style: Theme.of(
              context,
            ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 12),
          const Text(
            'O assistente envia apenas um resumo financeiro autorizado e mínimo para responder à sua pergunta. Ele não realiza transferências, não altera seus dados e pode estar indisponível sem conexão.',
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 20),
          FilledButton(onPressed: onAccept, child: const Text('Continuar')),
        ],
      ),
    ),
  );
}

class _WelcomePanel extends StatelessWidget {
  const _WelcomePanel({required this.onSuggestion});
  final Future<void> Function(String) onSuggestion;

  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(20),
    children: [
      Text(
        'Como posso ajudar?',
        style: Theme.of(
          context,
        ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
      ),
      const SizedBox(height: 8),
      const Text('As respostas explicam dados autorizados da sua conta.'),
      const SizedBox(height: 16),
      for (final suggestion in _FinancialAssistantPageState._suggestions)
        Padding(
          padding: const EdgeInsets.only(bottom: 8),
          child: OutlinedButton(
            onPressed: () => onSuggestion(suggestion),
            child: Text(suggestion),
          ),
        ),
    ],
  );
}

class _ChatBubble extends StatelessWidget {
  const _ChatBubble({
    required this.text,
    required this.assistant,
    this.system = false,
    this.loading = false,
  });
  final String text;
  final bool assistant;
  final bool system;
  final bool loading;

  @override
  Widget build(BuildContext context) => Align(
    alignment: assistant ? Alignment.centerLeft : Alignment.centerRight,
    child: Container(
      constraints: const BoxConstraints(maxWidth: 420),
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: system
            ? Theme.of(context).colorScheme.errorContainer
            : assistant
            ? Theme.of(context).colorScheme.surfaceContainerHighest
            : Theme.of(context).colorScheme.primaryContainer,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (loading) ...[
            const SizedBox(
              width: 16,
              height: 16,
              child: CircularProgressIndicator(strokeWidth: 2),
            ),
            const SizedBox(width: 8),
          ],
          Flexible(child: Text(text)),
        ],
      ),
    ),
  );
}

enum _ChatKind { user, assistant, system }

class _ChatMessage {
  const _ChatMessage(this.kind, this.text);
  final _ChatKind kind;
  final String text;

  factory _ChatMessage.user(String text) => _ChatMessage(_ChatKind.user, text);
  factory _ChatMessage.assistant(String text) =>
      _ChatMessage(_ChatKind.assistant, text);
  factory _ChatMessage.system(String text) =>
      _ChatMessage(_ChatKind.system, text);
}
