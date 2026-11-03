import 'package:flutter/material.dart';

import 'finance_repository.dart';

class PayYourselfFirstProjection {
  const PayYourselfFirstProjection({
    required this.incomeBasis,
    required this.suggestedReserve,
    required this.balanceAfterObligations,
    required this.distribution,
  });

  final double incomeBasis;
  final double suggestedReserve;
  final double balanceAfterObligations;
  final Map<String, double> distribution;

  bool get isAffordable => balanceAfterObligations >= 0;

  factory PayYourselfFirstProjection.calculate({
    required double expectedIncome,
    required double receivedIncome,
    required double essentialObligations,
    required double rate,
  }) {
    if (expectedIncome < 0 || receivedIncome < 0 || essentialObligations < 0) {
      throw ArgumentError('Valores monetários não podem ser negativos.');
    }
    final basis = _money(receivedIncome > 0 ? receivedIncome : expectedIncome);
    final reserve = _money(basis * rate);
    final allocation = <String, double>{
      'Reserva e renda fixa': _money(reserve * .7),
      'Fundos imobiliários': _money(reserve * .2),
      'Renda variável': _money(reserve * .1),
    };
    allocation['Reserva e renda fixa'] =
        _money(reserve - allocation.values.skip(1).fold(0, (a, b) => a + b));
    return PayYourselfFirstProjection(
      incomeBasis: basis,
      suggestedReserve: reserve,
      balanceAfterObligations: _money(basis - essentialObligations - reserve),
      distribution: allocation,
    );
  }

  static double _money(double value) => double.parse(value.toStringAsFixed(2));
}

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
  final _expectedIncome = TextEditingController();
  final _receivedIncome = TextEditingController();
  final _essentialObligations = TextEditingController();

  @override
  void dispose() {
    _expectedIncome.dispose();
    _receivedIncome.dispose();
    _essentialObligations.dispose();
    super.dispose();
  }

  double _readMoney(TextEditingController controller) =>
      double.tryParse(controller.text.replaceAll('.', '').replaceAll(',', '.')) ?? 0;

  PayYourselfFirstProjection get _projection => PayYourselfFirstProjection.calculate(
        expectedIncome: _readMoney(_expectedIncome),
        receivedIncome: _readMoney(_receivedIncome),
        essentialObligations: _readMoney(_essentialObligations),
        rate: _rate,
      );

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
    final projection = _projection;
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
          Text('Simulação mensal', style: Theme.of(context).textTheme.titleLarge),
          const Text('Informe valores para receber uma sugestão. Isto não registra uma receita nem movimenta dinheiro.'),
          TextField(controller: _expectedIncome, keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: const InputDecoration(labelText: 'Renda prevista', prefixText: 'R\$ '), onChanged: (_) => setState(() {})),
          TextField(controller: _receivedIncome, keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: const InputDecoration(labelText: 'Renda recebida', prefixText: 'R\$ '), onChanged: (_) => setState(() {})),
          TextField(controller: _essentialObligations, keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: const InputDecoration(labelText: 'Obrigações essenciais e vencidas', prefixText: 'R\$ '), onChanged: (_) => setState(() {})),
          const SizedBox(height: 12),
          Text('Base considerada: R\$ ${projection.incomeBasis.toStringAsFixed(2)}'),
          Text('Reserva sugerida: R\$ ${projection.suggestedReserve.toStringAsFixed(2)}'),
          for (final item in projection.distribution.entries)
            Text('${item.key}: R\$ ${item.value.toStringAsFixed(2)}'),
          if (!projection.isAffordable)
            const Padding(padding: EdgeInsets.only(top: 8), child: Text(
              'Atenção: a reserva sugerida compromete obrigações essenciais. Ajuste o percentual ou priorize as contas.',
              style: TextStyle(color: Colors.red),
            )),
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
