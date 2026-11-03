import 'package:flutter/material.dart';

import 'finance_repository.dart';

class PayYourselfFirstPage extends StatefulWidget {
  const PayYourselfFirstPage({
    required this.repository,
    required this.tenantId,
    super.key,
  });

  final FinanceRepository repository;
  final String tenantId;

  @override
  State<PayYourselfFirstPage> createState() => _PayYourselfFirstPageState();
}

class _PayYourselfFirstPageState extends State<PayYourselfFirstPage> {
  PayYourselfFirstPlanSummary? _plan;
  bool _loading = true;
  bool _saving = false;
  String? _error;
  double _rate = .2;
  double? _goalRate = .3;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final plan = await widget.repository.payYourselfFirstPlan(
        widget.tenantId,
      );
      if (!mounted) return;
      setState(() {
        _plan = plan;
        _rate = plan?.currentRate.toDouble() ?? .2;
        _goalRate = plan?.goalRate?.toDouble() ?? .3;
        _loading = false;
      });
    } catch (_) {
      if (mounted) {
        setState(() {
          _error = 'Não foi possível carregar o planejamento.';
          _loading = false;
        });
      }
    }
  }

  Future<void> _save() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Salvar planejamento?'),
        content: const Text(
          'Isso registra apenas sua intenção de reservar parte da renda. Nenhum investimento ou transferência será criado automaticamente.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Voltar'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Salvar'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) {
      return;
    }
    setState(() => _saving = true);
    try {
      final result = await widget.repository.configurePayYourselfFirstPlan(
        tenantId: widget.tenantId,
        existing: _plan,
        currentRate: _rate,
        goalRate: _goalRate,
        effectiveFrom: DateTime.now(),
      );
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            result == FinanceWriteState.pending
                ? 'Planejamento salvo neste dispositivo e aguardando sincronização.'
                : 'Planejamento salvo.',
          ),
        ),
      );
      await _load();
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Não foi possível salvar o planejamento.'),
          ),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _saving = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    if (_error != null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Primeiro Me Pago')),
        body: Center(child: Text(_error!)),
      );
    }
    final percentage = (_rate * 100).round();
    return Scaffold(
      appBar: AppBar(title: const Text('Primeiro Me Pago')),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          const Text(
            'Planeje a reserva antes de comprometer a renda. Esta configuração não movimenta dinheiro nem realiza aplicações.',
            style: TextStyle(fontSize: 16),
          ),
          const SizedBox(height: 24),
          Text(
            'Reserva planejada: $percentage%',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          Slider(
            value: _rate,
            min: .05,
            max: .5,
            divisions: 9,
            label: '$percentage%',
            onChanged: _saving
                ? null
                : (value) => setState(() {
                    _rate = value;
                    if (_goalRate != null && _goalRate! < value) {
                      _goalRate = value;
                    }
                  }),
          ),
          Wrap(
            spacing: 8,
            children: [
              for (final value in [.2, .3])
                ChoiceChip(
                  label: Text('${(value * 100).round()}%'),
                  selected: _rate == value,
                  onSelected: _saving
                      ? null
                      : (_) => setState(() {
                          _rate = value;
                          if (_goalRate != null && _goalRate! < value) {
                            _goalRate = value;
                          }
                        }),
                ),
            ],
          ),
          const SizedBox(height: 24),
          SwitchListTile(
            title: const Text('Meta de evolução'),
            subtitle: Text(
              _goalRate == null
                  ? 'Sem meta adicional.'
                  : 'Objetivo: ${(_goalRate! * 100).round()}% da renda.',
            ),
            value: _goalRate != null,
            onChanged: _saving
                ? null
                : (value) => setState(() => _goalRate = value ? .3 : null),
          ),
          if (_goalRate != null)
            Slider(
              value: _goalRate!,
              min: _rate,
              max: .5,
              divisions: 10,
              label: '${(_goalRate! * 100).round()}%',
              onChanged: _saving
                  ? null
                  : (value) => setState(() => _goalRate = value),
            ),
          const SizedBox(height: 24),
          FilledButton(
            onPressed: _saving ? null : _save,
            child: Text(_saving ? 'Salvando...' : 'Salvar planejamento'),
          ),
        ],
      ),
    );
  }
}
