import 'package:flutter/material.dart';

import 'finance_repository.dart';

class FinancialNotificationsPage extends StatefulWidget {
  const FinancialNotificationsPage({required this.repository, required this.tenantId, super.key});
  final FinanceRepository repository;
  final String tenantId;
  @override
  State<FinancialNotificationsPage> createState() => _FinancialNotificationsPageState();
}

class _FinancialNotificationsPageState extends State<FinancialNotificationsPage> {
  late Future<List<FinancialNotificationSummary>> _items = _load();
  Future<List<FinancialNotificationSummary>> _load() => widget.repository.notifications(widget.tenantId);
  Future<void> _refresh() async => setState(() => _items = _load());
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Notificações'), actions: [IconButton(onPressed: _refresh, tooltip: 'Atualizar', icon: const Icon(Icons.refresh))]),
    body: FutureBuilder<List<FinancialNotificationSummary>>(
      future: _items,
      builder: (context, snapshot) {
        if (snapshot.connectionState != ConnectionState.done) return const Center(child: CircularProgressIndicator());
        if (snapshot.hasError) return Center(child: Text('Não foi possível carregar notificações.'));
        final items = snapshot.data ?? const [];
        if (items.isEmpty) return const Center(child: Text('Nenhuma notificação por enquanto.'));
        return ListView.separated(
          itemCount: items.length, separatorBuilder: (_, _) => const Divider(height: 1),
          itemBuilder: (context, index) { final item = items[index]; return ListTile(
            leading: Icon(item.isRead ? Icons.notifications_none_outlined : Icons.notifications_active_outlined),
            title: Text(item.title, style: item.isRead ? null : const TextStyle(fontWeight: FontWeight.w700)),
            subtitle: Text(item.message),
            onTap: item.isRead ? null : () async { await widget.repository.markNotificationRead(tenantId: widget.tenantId, notification: item); if (mounted) _refresh(); },
          ); },
        );
      },
    ),
  );
}
