import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

import '../../core/network/api_client.dart';
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
    Map<String, double>? allocationWeights,
  }) {
    if (expectedIncome < 0 || receivedIncome < 0 || essentialObligations < 0) {
      throw ArgumentError('Valores monetários não podem ser negativos.');
    }
    final basis = _money(receivedIncome > 0 ? receivedIncome : expectedIncome);
    final reserve = _money(basis * rate);
    final weights = allocationWeights ?? _defaultAllocationWeights;
    final allocation = <String, double>{};
    final entries = weights.entries.toList(growable: false);
    for (var index = 0; index < entries.length; index++) {
      final entry = entries[index];
      allocation[entry.key] = index == 0
          ? 0
          : _money(reserve * entry.value / 100);
    }
    if (entries.isNotEmpty) {
      allocation[entries.first.key] = _money(
        reserve - allocation.values.skip(1).fold(0, (a, b) => a + b),
      );
    }
    return PayYourselfFirstProjection(
      incomeBasis: basis,
      suggestedReserve: reserve,
      balanceAfterObligations: _money(basis - essentialObligations - reserve),
      distribution: allocation,
    );
  }

  static double _money(double value) => double.parse(value.toStringAsFixed(2));

  static const _defaultAllocationWeights = <String, double>{
    'Reserva e renda fixa': 70,
    'Fundos imobiliários': 20,
    'Renda variável': 10,
  };
}

class _PortfolioDraft {
  const _PortfolioDraft({
    required this.id,
    required this.investmentAssetId,
    required this.key,
    required this.assetClass,
    required this.weight,
    required this.version,
  });

  final String? id;
  final String? investmentAssetId;
  final String key;
  final String assetClass;
  final double weight;
  final int? version;

  bool get isPendingLocal => id?.startsWith('local-') ?? false;

  _PortfolioDraft copyWith({double? weight}) => _PortfolioDraft(
        id: id,
        investmentAssetId: investmentAssetId,
        key: key,
        assetClass: assetClass,
        weight: weight ?? this.weight,
        version: version,
      );

  TargetPortfolioAllocationSummary toSummary() =>
      TargetPortfolioAllocationSummary(
        id: id ?? 'local-unsaved-$key',
        investmentAssetId: investmentAssetId,
        key: key,
        assetClass: assetClass,
        weight: weight,
        version: version ?? 0,
      );

  factory _PortfolioDraft.fromSummary(TargetPortfolioAllocationSummary item) =>
      _PortfolioDraft(
        id: item.id,
        investmentAssetId: item.investmentAssetId,
        key: item.key,
        assetClass: item.assetClass,
        weight: item.weight.toDouble(),
        version: item.version,
      );
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
  bool _savingDistribution = false;
  String? _error;
  double _rate = .2;
  double? _goalRate = .3;
  List<_PortfolioDraft> _portfolio = _defaultPortfolio();
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
        allocationWeights: {
          for (final allocation in _portfolio) allocation.key: allocation.weight,
        },
      );

  bool get _portfolioPending => _portfolio.any((item) => item.isPendingLocal);
  double get _portfolioTotal =>
      _portfolio.fold(0, (total, allocation) => total + allocation.weight);

  static List<_PortfolioDraft> _defaultPortfolio() => const [
        _PortfolioDraft(
          id: null,
          investmentAssetId: null,
          key: 'Reserva e renda fixa',
          assetClass: 'FixedIncome',
          weight: 70,
          version: null,
        ),
        _PortfolioDraft(
          id: null,
          investmentAssetId: null,
          key: 'Fundos imobiliários',
          assetClass: 'RealEstateFund',
          weight: 20,
          version: null,
        ),
        _PortfolioDraft(
          id: null,
          investmentAssetId: null,
          key: 'Renda variável',
          assetClass: 'Equity',
          weight: 10,
          version: null,
        ),
      ];

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final values = await Future.wait([
        widget.repository.payYourselfFirstPlan(widget.tenantId),
        widget.repository.targetPortfolio(widget.tenantId),
      ]);
      final plan = values[0] as PayYourselfFirstPlanSummary?;
      final targetPortfolio = values[1] as List<TargetPortfolioAllocationSummary>;
      if (!mounted) return;
      setState(() {
        _plan = plan;
        _rate = plan?.currentRate.toDouble() ?? .2;
        _goalRate = plan?.goalRate?.toDouble() ?? .3;
        _portfolio = targetPortfolio.isEmpty
            ? _defaultPortfolio()
            : targetPortfolio.map(_PortfolioDraft.fromSummary).toList();
        _loading = false;
      });
    } on ApiException catch (error) {
      if (mounted) {
        setState(() {
          _error = error.message;
          _loading = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _error = 'Não foi possível carregar o planejamento.';
          _loading = false;
        });
      }
    }
  }

  Future<void> _saveDistribution() async {
    if (_portfolioPending) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('A distribuição aguarda sincronização antes de uma nova alteração.'),
        ),
      );
      return;
    }
    if ((_portfolioTotal - 100).abs() > .001) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('A distribuição deve totalizar exatamente 100%.')),
      );
      return;
    }
    setState(() => _savingDistribution = true);
    try {
      final result = await widget.repository.setTargetPortfolio(
        tenantId: widget.tenantId,
        allocations: _portfolio.map((item) => item.toSummary()).toList(),
      );
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            result == FinanceWriteState.pending
                ? 'Distribuição salva neste dispositivo e aguardando sincronização.'
                : 'Distribuição planejada salva.',
          ),
        ),
      );
      await _load();
    } on ApiException catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(error.message)));
      }
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Não foi possível salvar a distribuição.')),
        );
      }
    } finally {
      if (mounted) setState(() => _savingDistribution = false);
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
      // A plan may also be changed by another device. Refresh only the
      // concurrency token immediately before the explicit user action so a
      // stale screen never overwrites server state. The selected rates below
      // remain the values currently shown to the user.
      final latestPlan = await widget.repository.payYourselfFirstPlan(
        widget.tenantId,
        allowCachedFallback: false,
      );
      if (!mounted) return;
      _plan = latestPlan;
      if (kDebugMode) {
        debugPrint(
          'RASS_PYP_SAVE_PRECHECK: planPresent=${latestPlan != null} '
          'version=${latestPlan?.version}',
        );
      }
      final result = await widget.repository.configurePayYourselfFirstPlan(
        tenantId: widget.tenantId,
        existing: latestPlan,
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
    } on ApiException catch (error) {
      if (kDebugMode) {
        final correlation = error.correlationId == null
            ? ''
            : ' correlationId=${error.correlationId}';
        debugPrint(
          'RASS_PYP_SAVE_FAILURE: failure=${error.failure.name} '
          'status=${error.statusCode}$correlation',
        );
      }
      if (error.failure == ApiFailure.conflict) {
        await _load();
        if (kDebugMode) {
          debugPrint(
            'RASS_PYP_CONFLICT_REFRESH: planPresent=${_plan != null} '
            'version=${_plan?.version}',
          );
        }
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text(
                'O planejamento foi atualizado em outro momento. Recarregamos os dados; revise e salve novamente.',
              ),
            ),
          );
        }
      } else if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(error.message)),
        );
      }
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
          Text('Distribuição planejada', style: Theme.of(context).textTheme.titleLarge),
          const Text(
            'Define como a reserva sugerida será distribuída. Salvar esta configuração não realiza investimento, transferência ou pagamento.',
          ),
          for (var index = 0; index < _portfolio.length; index++)
            TextFormField(
              key: ValueKey('portfolio-weight-${_portfolio[index].key}'),
              initialValue: _portfolio[index].weight.toStringAsFixed(2),
              enabled: !_savingDistribution && !_portfolioPending,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: InputDecoration(
                labelText: _portfolio[index].key,
                suffixText: '%',
              ),
              onChanged: (value) {
                final weight = double.tryParse(value.replaceAll(',', '.'));
                if (weight == null || weight < 0) return;
                setState(() {
                  _portfolio[index] = _portfolio[index].copyWith(weight: weight);
                });
              },
            ),
          Text('Total configurado: ${_portfolioTotal.toStringAsFixed(2)}%'),
          if (_portfolioPending)
            const Padding(
              padding: EdgeInsets.only(top: 8),
              child: Text(
                'Distribuição salva localmente e aguardando sincronização.',
                style: TextStyle(color: Colors.orange),
              ),
            ),
          const SizedBox(height: 8),
          OutlinedButton(
            onPressed: _savingDistribution || _portfolioPending
                ? null
                : _saveDistribution,
            child: Text(
              _savingDistribution ? 'Salvando distribuição...' : 'Salvar distribuição',
            ),
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
