import 'dart:convert';
import 'dart:math';

import 'package:drift/drift.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'offline_database.dart';

class PendingFinanceOperation {
  const PendingFinanceOperation({
    required this.operationId,
    required this.tenantId,
    required this.endpoint,
    required this.payload,
    required this.entityType,
    required this.entityId,
    required this.operationType,
  });

  final String operationId;
  final String tenantId;
  final String endpoint;
  final Map<String, dynamic> payload;
  final String entityType;
  final String entityId;
  final String operationType;
}

class SyncConflict {
  const SyncConflict({
    required this.operationId,
    required this.entityType,
    required this.entityId,
    required this.operationType,
    required this.safeError,
  });

  final String operationId;
  final String entityType;
  final String entityId;
  final String operationType;
  final String safeError;
}

class RemoteSyncChange {
  const RemoteSyncChange({
    required this.operationId,
    required this.resourceId,
    required this.eventType,
  });

  final String operationId;
  final String resourceId;
  final String eventType;
}

class OfflineFinanceStore {
  OfflineFinanceStore._(this._database, this.subject, this._deviceId);

  OfflineFinanceStore.forTesting(
    OfflineDatabase database, {
    required this.subject,
    this._deviceId = 'test-device',
  }) : _database = database;

  static const _deviceKey = 'rassapp.offline.device-id.v1';

  final OfflineDatabase _database;
  final String subject;
  final String _deviceId;

  static Future<OfflineFinanceStore> open(String subject) async {
    final storage = const FlutterSecureStorage();
    var deviceId = await storage.read(key: _deviceKey);
    if (deviceId == null) {
      deviceId = _newUuid();
      await storage.write(key: _deviceKey, value: deviceId);
    }
    return OfflineFinanceStore._(
      await OfflineDatabase.open(subject: subject),
      subject,
      deviceId,
    );
  }

  static Future<void> deleteForSubject(String subject) =>
      OfflineDatabase.deleteForSubject(subject);

  Future<void> cacheAll(
    String tenantId,
    String entityType,
    Iterable<Map<String, dynamic>> resources,
  ) => _database.transaction(() async {
    final existing =
        await (_database.select(_database.cachedResources)..where(
              (row) =>
                  row.subject.equals(subject) &
                  row.tenantId.equals(tenantId) &
                  row.entityType.equals(entityType),
            ))
            .get();
    final pendingLocalIds =
        await (_database.select(_database.pendingSyncOperations)..where(
              (row) =>
                  row.subject.equals(subject) &
                  row.tenantId.equals(tenantId) &
                  row.entityType.equals(entityType) &
                  (row.status.equals('pending') |
                      row.status.equals('conflict')),
            ))
            .get();
    final pendingIds = pendingLocalIds.map((row) => row.entityId).toSet();
    final localResources = existing
        .where((row) => pendingIds.contains(row.entityId))
        .map((row) => jsonDecode(row.payloadJson) as Map<String, dynamic>)
        .toList(growable: false);
    await (_database.delete(_database.cachedResources)..where(
          (row) =>
              row.subject.equals(subject) &
              row.tenantId.equals(tenantId) &
              row.entityType.equals(entityType),
        ))
        .go();
    await _database.batch((batch) {
      batch.insertAll(
        _database.cachedResources,
        [...localResources, ...resources].map(
          (resource) => CachedResourcesCompanion.insert(
            subject: subject,
            tenantId: tenantId,
            entityType: entityType,
            entityId: resource['id'] as String,
            payloadJson: jsonEncode(resource),
            version: Value((resource['version'] as num?)?.toInt() ?? 0),
            updatedAt: DateTime.now().toUtc(),
          ),
        ),
      );
    });
  });

  Future<List<Map<String, dynamic>>> cached(
    String tenantId,
    String entityType,
  ) async {
    final rows =
        await (_database.select(_database.cachedResources)
              ..where(
                (row) =>
                    row.subject.equals(subject) &
                    row.tenantId.equals(tenantId) &
                    row.entityType.equals(entityType) &
                    row.isDeleted.equals(false),
              )
              ..orderBy([(row) => OrderingTerm.asc(row.updatedAt)]))
            .get();
    return rows
        .map((row) => jsonDecode(row.payloadJson) as Map<String, dynamic>)
        .toList(growable: false);
  }

  Future<void> cacheOne(
    String tenantId,
    String entityType,
    Map<String, dynamic> resource,
  ) => _database
      .into(_database.cachedResources)
      .insertOnConflictUpdate(
        CachedResourcesCompanion.insert(
          subject: subject,
          tenantId: tenantId,
          entityType: entityType,
          entityId: resource['id'] as String,
          payloadJson: jsonEncode(resource),
          version: Value((resource['version'] as num?)?.toInt() ?? 0),
          updatedAt: DateTime.now().toUtc(),
        ),
      );

  Future<void> enqueue({
    required String tenantId,
    required String endpoint,
    required Map<String, dynamic> payload,
    required String entityType,
    required String entityId,
    required String operationType,
  }) {
    final operationId = payload['operationId'] as String;
    final now = DateTime.now().toUtc();
    return _database
        .into(_database.pendingSyncOperations)
        .insert(
          PendingSyncOperationsCompanion.insert(
            operationId: operationId,
            subject: subject,
            tenantId: tenantId,
            deviceId: _deviceId,
            entityId: entityId,
            entityType: entityType,
            operationType: operationType,
            endpoint: endpoint,
            payloadJson: jsonEncode(payload),
            clientTimestamp: now,
          ),
          mode: InsertMode.insertOrIgnore,
        );
  }

  Future<List<PendingFinanceOperation>> pending(String tenantId) async {
    final now = DateTime.now().toUtc();
    final rows =
        await (_database.select(_database.pendingSyncOperations)
              ..where(
                (row) =>
                    row.subject.equals(subject) &
                    row.tenantId.equals(tenantId) &
                    row.status.equals('pending') &
                    (row.nextAttemptAt.isNull() |
                        row.nextAttemptAt.isSmallerOrEqualValue(now)),
              )
              ..orderBy([(row) => OrderingTerm.asc(row.clientTimestamp)])
              ..limit(25))
            .get();
    return rows
        .map(
          (row) => PendingFinanceOperation(
            operationId: row.operationId,
            tenantId: row.tenantId,
            endpoint: row.endpoint,
            payload: jsonDecode(row.payloadJson) as Map<String, dynamic>,
            entityType: row.entityType,
            entityId: row.entityId,
            operationType: row.operationType,
          ),
        )
        .toList(growable: false);
  }

  Future<void> acknowledge(String operationId) => (_database.delete(
    _database.pendingSyncOperations,
  )..where((row) => row.operationId.equals(operationId))).go();

  Future<void> defer(
    String operationId, {
    required String safeError,
    required bool retryable,
  }) async {
    final operation = await (_database.select(
      _database.pendingSyncOperations,
    )..where((row) => row.operationId.equals(operationId))).getSingleOrNull();
    if (operation == null) return;
    final retries = operation.retryCount + 1;
    final delay = retryable
        ? Duration(seconds: min(300, 2 << min(retries, 7)))
        : null;
    await (_database.update(
      _database.pendingSyncOperations,
    )..where((row) => row.operationId.equals(operationId))).write(
      PendingSyncOperationsCompanion(
        status: Value(retryable ? 'pending' : 'conflict'),
        retryCount: Value(retries),
        lastError: Value(safeError),
        nextAttemptAt: Value(
          delay == null ? null : DateTime.now().toUtc().add(delay),
        ),
      ),
    );
  }

  Future<int> pendingCount(String tenantId) async {
    final count = _database.pendingSyncOperations.operationId.count();
    final query = _database.selectOnly(_database.pendingSyncOperations)
      ..addColumns([count])
      ..where(
        _database.pendingSyncOperations.subject.equals(subject) &
            _database.pendingSyncOperations.tenantId.equals(tenantId) &
            _database.pendingSyncOperations.status.equals('pending'),
      );
    return (await query.getSingle()).read(count) ?? 0;
  }

  Future<List<SyncConflict>> conflicts(String tenantId) async {
    final rows =
        await (_database.select(_database.pendingSyncOperations)
              ..where(
                (row) =>
                    row.subject.equals(subject) &
                    row.tenantId.equals(tenantId) &
                    row.status.equals('conflict'),
              )
              ..orderBy([(row) => OrderingTerm.asc(row.clientTimestamp)]))
            .get();
    return rows
        .map(
          (row) => SyncConflict(
            operationId: row.operationId,
            entityType: row.entityType,
            entityId: row.entityId,
            operationType: row.operationType,
            safeError: row.lastError ?? 'A operação precisa de revisão.',
          ),
        )
        .toList(growable: false);
  }

  Future<void> discardConflict(String tenantId, String operationId) =>
      (_database.delete(_database.pendingSyncOperations)..where(
            (row) =>
                row.subject.equals(subject) &
                row.tenantId.equals(tenantId) &
                row.operationId.equals(operationId) &
                row.status.equals('conflict'),
          ))
          .go();

  Future<String?> cursor(String tenantId) async {
    final state =
        await (_database.select(_database.syncStates)..where(
              (row) =>
                  row.subject.equals(subject) & row.tenantId.equals(tenantId),
            ))
            .getSingleOrNull();
    return state?.cursor;
  }

  Future<void> setCursor(String tenantId, String cursor) => _database
      .into(_database.syncStates)
      .insertOnConflictUpdate(
        SyncStatesCompanion.insert(
          subject: subject,
          tenantId: tenantId,
          cursor: Value(cursor),
          updatedAt: DateTime.now().toUtc(),
        ),
      );

  Future<void> rememberInbox(String tenantId, Iterable<String> operationIds) =>
      _database.batch((batch) {
        batch.insertAll(
          _database.syncInbox,
          operationIds.map(
            (operationId) => SyncInboxCompanion.insert(
              operationId: operationId,
              subject: subject,
              tenantId: tenantId,
              receivedAt: DateTime.now().toUtc(),
            ),
          ),
          mode: InsertMode.insertOrIgnore,
        );
      });

  Future<void> applyRemotePage({
    required String tenantId,
    required Iterable<RemoteSyncChange> changes,
    required Map<String, List<Map<String, dynamic>>> snapshots,
    required String? appliedCursor,
  }) => _database.transaction(() async {
    await _database.batch((batch) {
      batch.insertAll(
        _database.syncInbox,
        changes.map(
          (change) => SyncInboxCompanion.insert(
            operationId: change.operationId,
            subject: subject,
            tenantId: tenantId,
            receivedAt: DateTime.now().toUtc(),
          ),
        ),
        mode: InsertMode.insertOrIgnore,
      );
    });

    for (final entry in snapshots.entries) {
      final existing =
          await (_database.select(_database.cachedResources)..where(
                (row) =>
                    row.subject.equals(subject) &
                    row.tenantId.equals(tenantId) &
                    row.entityType.equals(entry.key),
              ))
              .get();
      final localResources = existing
          .where((row) => row.entityId.startsWith('local-'))
          .map((row) => jsonDecode(row.payloadJson) as Map<String, dynamic>);
      await (_database.delete(_database.cachedResources)..where(
            (row) =>
                row.subject.equals(subject) &
                row.tenantId.equals(tenantId) &
                row.entityType.equals(entry.key),
          ))
          .go();
      await _database.batch((batch) {
        batch.insertAll(
          _database.cachedResources,
          [...localResources, ...entry.value].map(
            (resource) => CachedResourcesCompanion.insert(
              subject: subject,
              tenantId: tenantId,
              entityType: entry.key,
              entityId: resource['id'] as String,
              payloadJson: jsonEncode(resource),
              version: Value((resource['version'] as num?)?.toInt() ?? 0),
              updatedAt: DateTime.now().toUtc(),
            ),
          ),
          mode: InsertMode.insertOrReplace,
        );
      });
    }

    if (appliedCursor != null) {
      await _database
          .into(_database.syncStates)
          .insertOnConflictUpdate(
            SyncStatesCompanion.insert(
              subject: subject,
              tenantId: tenantId,
              cursor: Value(appliedCursor),
              updatedAt: DateTime.now().toUtc(),
            ),
          );
    }
  });

  Future<void> close() => _database.close();
}

String _newUuid() {
  final random = Random.secure();
  final bytes = List<int>.generate(16, (_) => random.nextInt(256));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  final hex = bytes
      .map((value) => value.toRadixString(16).padLeft(2, '0'))
      .join();
  return '${hex.substring(0, 8)}-${hex.substring(8, 12)}-${hex.substring(12, 16)}-${hex.substring(16, 20)}-${hex.substring(20)}';
}
