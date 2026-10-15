import 'package:flutter/material.dart';

import '../auth/auth_controller.dart';
import 'finance_home_shell.dart';

class HomePage extends StatefulWidget {
  const HomePage({required this.authController, super.key});
  final AuthController authController;
  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  int _index = 0;
  @override
  Widget build(BuildContext context) =>
      FinanceHomeShell(authController: widget.authController);

  Widget buildLegacyPlaceholder(BuildContext context) {
    const labels = ['Dashboard', 'Transações', 'Contas', 'Menu'];
    return Scaffold(
      appBar: AppBar(title: Text(labels[_index])),
      body: _index == 0
          ? const _Dashboard()
          : _Placeholder(title: labels[_index]),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _index,
        onDestinationSelected: (value) => setState(() => _index = value),
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.dashboard_outlined),
            selectedIcon: Icon(Icons.dashboard),
            label: 'Início',
          ),
          NavigationDestination(
            icon: Icon(Icons.receipt_long_outlined),
            selectedIcon: Icon(Icons.receipt_long),
            label: 'Transações',
          ),
          NavigationDestination(
            icon: Icon(Icons.account_balance_wallet_outlined),
            selectedIcon: Icon(Icons.account_balance_wallet),
            label: 'Contas',
          ),
          NavigationDestination(
            icon: Icon(Icons.menu_outlined),
            selectedIcon: Icon(Icons.menu),
            label: 'Menu',
          ),
        ],
      ),
      floatingActionButton: _index == 3
          ? FloatingActionButton.small(
              onPressed: widget.authController.signOut,
              tooltip: 'Sair',
              child: const Icon(Icons.logout),
            )
          : null,
    );
  }
}

class _Dashboard extends StatelessWidget {
  const _Dashboard();
  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(20),
    children: [
      Text(
        'Resumo financeiro',
        style: Theme.of(
          context,
        ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
      ),
      const SizedBox(height: 14),
      const _MetricCard(title: 'Patrimônio líquido', value: 'R\$ --'),
      const SizedBox(height: 12),
      const Row(
        children: [
          Expanded(
            child: _MetricCard(title: 'Receitas', value: 'R\$ --'),
          ),
          SizedBox(width: 12),
          Expanded(
            child: _MetricCard(title: 'Despesas', value: 'R\$ --'),
          ),
        ],
      ),
      const SizedBox(height: 24),
      const _Placeholder(
        title:
            'Os dados do dashboard serão carregados após a seleção segura do tenant.',
      ),
    ],
  );
}

class _MetricCard extends StatelessWidget {
  const _MetricCard({required this.title, required this.value});
  final String title;
  final String value;
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title),
          const SizedBox(height: 8),
          Text(
            value,
            style: Theme.of(
              context,
            ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
          ),
        ],
      ),
    ),
  );
}

class _Placeholder extends StatelessWidget {
  const _Placeholder({required this.title});
  final String title;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(28),
      child: Text(title, textAlign: TextAlign.center),
    ),
  );
}
