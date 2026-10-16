import 'dart:convert';
import 'dart:io';
import 'dart:math';

import 'package:crypto/crypto.dart';
import 'package:drift/drift.dart';
import 'package:drift/native.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:path/path.dart' as path;
import 'package:path_provider/path_provider.dart';

part 'offline_database.g.dart';

class CachedResources extends Table {
  TextColumn get subject => text()();
  TextColumn get tenantId => text()();
  TextColumn get entityType => text()();
  TextColumn get entityId => text()();
  TextColumn get payloadJson => text()();
  IntColumn get version => integer().withDefault(const Constant(0))();
  BoolColumn get isDeleted => boolean().withDefault(const Constant(false))();
  DateTimeColumn get updatedAt => dateTime()();

  @override
  Set<Column<Object>> get primaryKey => {
    subject,
    tenantId,
    entityType,
    entityId,
  };
}

class PendingSyncOperations extends Table {
  TextColumn get operationId => text()();
  TextColumn get subject => text()();
  TextColumn get tenantId => text()();
  TextColumn get deviceId => text()();
  TextColumn get entityId => text()();
  TextColumn get entityType => text()();
  TextColumn get operationType => text()();
  TextColumn get endpoint => text()();
  TextColumn get payloadJson => text()();
  IntColumn get version => integer().withDefault(const Constant(0))();
  TextColumn get status => text().withDefault(const Constant('pending'))();
  IntColumn get retryCount => integer().withDefault(const Constant(0))();
  TextColumn get lastError => text().nullable()();
  DateTimeColumn get clientTimestamp => dateTime()();
  DateTimeColumn get nextAttemptAt => dateTime().nullable()();

  @override
  Set<Column<Object>> get primaryKey => {operationId};
}

class SyncInbox extends Table {
  TextColumn get operationId => text()();
  TextColumn get subject => text()();
  TextColumn get tenantId => text()();
  DateTimeColumn get receivedAt => dateTime()();

  @override
  Set<Column<Object>> get primaryKey => {subject, tenantId, operationId};
}

class SyncStates extends Table {
  TextColumn get subject => text()();
  TextColumn get tenantId => text()();
  TextColumn get cursor => text().nullable()();
  DateTimeColumn get updatedAt => dateTime()();

  @override
  Set<Column<Object>> get primaryKey => {subject, tenantId};
}

@DriftDatabase(
  tables: [CachedResources, PendingSyncOperations, SyncInbox, SyncStates],
)
class OfflineDatabase extends _$OfflineDatabase {
  OfflineDatabase(super.executor);

  static Future<OfflineDatabase> open({required String subject}) async {
    final storage = const FlutterSecureStorage();
    final keyName = 'rassapp.offline.database-key.$subject';
    var encryptionKey = await storage.read(key: keyName);
    if (encryptionKey == null) {
      encryptionKey = _newEncryptionKey();
      await storage.write(key: keyName, value: encryptionKey);
    }

    final documents = await getApplicationDocumentsDirectory();
    final databaseId = sha256.convert(utf8.encode(subject)).toString();
    final databaseFile = File(
      path.join(documents.path, 'finance-$databaseId.sqlite'),
    );
    return OfflineDatabase(
      NativeDatabase.createInBackground(
        databaseFile,
        setup: (database) {
          if (database.select('PRAGMA cipher;').isEmpty) {
            throw StateError(
              'A biblioteca de criptografia local não está disponível.',
            );
          }
          database.execute("PRAGMA key = '${_escapeSql(encryptionKey!)}';");
        },
      ),
    );
  }

  static Future<void> deleteForSubject(String subject) async {
    final documents = await getApplicationDocumentsDirectory();
    final databaseId = sha256.convert(utf8.encode(subject)).toString();
    final file = File(path.join(documents.path, 'finance-$databaseId.sqlite'));
    if (await file.exists()) await file.delete();
    await const FlutterSecureStorage().delete(
      key: 'rassapp.offline.database-key.$subject',
    );
  }

  @override
  int get schemaVersion => 2;

  @override
  MigrationStrategy get migration => MigrationStrategy(
    onCreate: (migrator) => migrator.createAll(),
    onUpgrade: (migrator, from, to) async {
      if (from < 2) {
        await customStatement('ALTER TABLE sync_inbox RENAME TO sync_inbox_v1');
        await migrator.createTable(syncInbox);
        await customStatement('''
          INSERT INTO sync_inbox (operation_id, subject, tenant_id, received_at)
          SELECT operation_id, subject, tenant_id, received_at
          FROM sync_inbox_v1
        ''');
        await customStatement('DROP TABLE sync_inbox_v1');
      }
    },
    beforeOpen: (details) async {
      await customStatement('PRAGMA foreign_keys = ON');
    },
  );
}

String _newEncryptionKey() {
  final random = Random.secure();
  final bytes = List<int>.generate(32, (_) => random.nextInt(256));
  return base64UrlEncode(bytes);
}

String _escapeSql(String value) => value.replaceAll("'", "''");
