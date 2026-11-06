import 'package:flutter/material.dart';

import '../../core/network/api_client.dart';
import '../auth/auth_controller.dart';

class EntitlementsSnapshot {
  const EntitlementsSnapshot({required this.plan, required this.entitlements});

  final String plan;
  final List<PlanEntitlement> entitlements;

  factory EntitlementsSnapshot.fromJson(Object? value) {
    if (value is! Map<String, dynamic> ||
        value['plan'] is! String ||
        value['entitlements'] is! List) {
      throw const ApiException(
        ApiFailure.unexpected,
        'Não foi possível interpretar os direitos do plano.',
      );
    }
    return EntitlementsSnapshot(
      plan: value['plan'] as String,
      entitlements: (value['entitlements'] as List)
          .map(PlanEntitlement.fromJson)
          .toList(growable: false),
    );
  }
}

class PlanEntitlement {
  const PlanEntitlement({
    required this.feature,
    required this.enabled,
    required this.limit,
  });

  final String feature;
  final bool enabled;
  final int? limit;

  factory PlanEntitlement.fromJson(Object? value) {
    if (value is! Map<String, dynamic> ||
        value['feature'] is! String ||
        value['enabled'] is! bool ||
        (value['limit'] != null && value['limit'] is! int)) {
      throw const ApiException(
        ApiFailure.unexpected,
        'Não foi possível interpretar um recurso do plano.',
      );
    }
    return PlanEntitlement(
      feature: value['feature'] as String,
      enabled: value['enabled'] as bool,
      limit: value['limit'] as int?,
    );
  }
}

class EntitlementsPage extends StatefulWidget {
  const EntitlementsPage({required this.authController, super.key});

  final AuthController authController;

  @override
  State<EntitlementsPage> createState() => _EntitlementsPageState();
}

class _EntitlementsPageState extends State<EntitlementsPage> {
  late Future<EntitlementsSnapshot> _future;

  @override
  void initState() {
    super.initState();
    _future = _load();
  }

  Future<EntitlementsSnapshot> _load() async {
    final response = await widget.authController.getAuthenticated(
      '/api/v1/monetization/entitlements',
    );
    return EntitlementsSnapshot.fromJson(response);
  }

  void _reload() {
    setState(() => _future = _load());
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Plano e recursos')),
    body: FutureBuilder<EntitlementsSnapshot>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState != ConnectionState.done) {
          return const Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError) {
          return Center(
            child: OutlinedButton(
              onPressed: _reload,
              child: const Text('Tentar novamente'),
            ),
          );
        }
        final data = snapshot.requireData;
        return ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Card(
              child: ListTile(
                leading: const Icon(Icons.workspace_premium_outlined),
                title: Text(data.plan),
                subtitle: const Text('Acesso calculado pelo servidor.'),
              ),
            ),
            for (final item in data.entitlements)
              Card(
                child: ListTile(
                  leading: Icon(
                    item.enabled
                        ? Icons.check_circle_outline
                        : Icons.lock_outline,
                  ),
                  title: Text(item.feature),
                  subtitle: Text(
                    item.enabled
                        ? item.limit == null
                              ? 'Disponível'
                              : 'Limite atual: ${item.limit}'
                        : 'Indisponível no seu plano atual',
                  ),
                ),
              ),
            const Padding(
              padding: EdgeInsets.all(16),
              child: Text(
                'Compras e restauração de assinatura ainda não estão disponíveis.',
                textAlign: TextAlign.center,
              ),
            ),
          ],
        );
      },
    ),
  );
}
