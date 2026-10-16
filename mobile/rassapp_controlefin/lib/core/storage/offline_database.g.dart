// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'offline_database.dart';

// ignore_for_file: type=lint
class $CachedResourcesTable extends CachedResources
    with TableInfo<$CachedResourcesTable, CachedResource> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $CachedResourcesTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _subjectMeta = const VerificationMeta(
    'subject',
  );
  @override
  late final GeneratedColumn<String> subject = GeneratedColumn<String>(
    'subject',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _tenantIdMeta = const VerificationMeta(
    'tenantId',
  );
  @override
  late final GeneratedColumn<String> tenantId = GeneratedColumn<String>(
    'tenant_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _entityTypeMeta = const VerificationMeta(
    'entityType',
  );
  @override
  late final GeneratedColumn<String> entityType = GeneratedColumn<String>(
    'entity_type',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _entityIdMeta = const VerificationMeta(
    'entityId',
  );
  @override
  late final GeneratedColumn<String> entityId = GeneratedColumn<String>(
    'entity_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _payloadJsonMeta = const VerificationMeta(
    'payloadJson',
  );
  @override
  late final GeneratedColumn<String> payloadJson = GeneratedColumn<String>(
    'payload_json',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _versionMeta = const VerificationMeta(
    'version',
  );
  @override
  late final GeneratedColumn<int> version = GeneratedColumn<int>(
    'version',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: false,
    defaultValue: const Constant(0),
  );
  static const VerificationMeta _isDeletedMeta = const VerificationMeta(
    'isDeleted',
  );
  @override
  late final GeneratedColumn<bool> isDeleted = GeneratedColumn<bool>(
    'is_deleted',
    aliasedName,
    false,
    type: DriftSqlType.bool,
    requiredDuringInsert: false,
    defaultConstraints: GeneratedColumn.constraintIsAlways(
      'CHECK ("is_deleted" IN (0, 1))',
    ),
    defaultValue: const Constant(false),
  );
  static const VerificationMeta _updatedAtMeta = const VerificationMeta(
    'updatedAt',
  );
  @override
  late final GeneratedColumn<DateTime> updatedAt = GeneratedColumn<DateTime>(
    'updated_at',
    aliasedName,
    false,
    type: DriftSqlType.dateTime,
    requiredDuringInsert: true,
  );
  @override
  List<GeneratedColumn> get $columns => [
    subject,
    tenantId,
    entityType,
    entityId,
    payloadJson,
    version,
    isDeleted,
    updatedAt,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'cached_resources';
  @override
  VerificationContext validateIntegrity(
    Insertable<CachedResource> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('subject')) {
      context.handle(
        _subjectMeta,
        subject.isAcceptableOrUnknown(data['subject']!, _subjectMeta),
      );
    } else if (isInserting) {
      context.missing(_subjectMeta);
    }
    if (data.containsKey('tenant_id')) {
      context.handle(
        _tenantIdMeta,
        tenantId.isAcceptableOrUnknown(data['tenant_id']!, _tenantIdMeta),
      );
    } else if (isInserting) {
      context.missing(_tenantIdMeta);
    }
    if (data.containsKey('entity_type')) {
      context.handle(
        _entityTypeMeta,
        entityType.isAcceptableOrUnknown(data['entity_type']!, _entityTypeMeta),
      );
    } else if (isInserting) {
      context.missing(_entityTypeMeta);
    }
    if (data.containsKey('entity_id')) {
      context.handle(
        _entityIdMeta,
        entityId.isAcceptableOrUnknown(data['entity_id']!, _entityIdMeta),
      );
    } else if (isInserting) {
      context.missing(_entityIdMeta);
    }
    if (data.containsKey('payload_json')) {
      context.handle(
        _payloadJsonMeta,
        payloadJson.isAcceptableOrUnknown(
          data['payload_json']!,
          _payloadJsonMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_payloadJsonMeta);
    }
    if (data.containsKey('version')) {
      context.handle(
        _versionMeta,
        version.isAcceptableOrUnknown(data['version']!, _versionMeta),
      );
    }
    if (data.containsKey('is_deleted')) {
      context.handle(
        _isDeletedMeta,
        isDeleted.isAcceptableOrUnknown(data['is_deleted']!, _isDeletedMeta),
      );
    }
    if (data.containsKey('updated_at')) {
      context.handle(
        _updatedAtMeta,
        updatedAt.isAcceptableOrUnknown(data['updated_at']!, _updatedAtMeta),
      );
    } else if (isInserting) {
      context.missing(_updatedAtMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {
    subject,
    tenantId,
    entityType,
    entityId,
  };
  @override
  CachedResource map(Map<String, dynamic> data, {String? tablePrefix}) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return CachedResource(
      subject: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}subject'],
      )!,
      tenantId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}tenant_id'],
      )!,
      entityType: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}entity_type'],
      )!,
      entityId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}entity_id'],
      )!,
      payloadJson: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}payload_json'],
      )!,
      version: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}version'],
      )!,
      isDeleted: attachedDatabase.typeMapping.read(
        DriftSqlType.bool,
        data['${effectivePrefix}is_deleted'],
      )!,
      updatedAt: attachedDatabase.typeMapping.read(
        DriftSqlType.dateTime,
        data['${effectivePrefix}updated_at'],
      )!,
    );
  }

  @override
  $CachedResourcesTable createAlias(String alias) {
    return $CachedResourcesTable(attachedDatabase, alias);
  }
}

class CachedResource extends DataClass implements Insertable<CachedResource> {
  final String subject;
  final String tenantId;
  final String entityType;
  final String entityId;
  final String payloadJson;
  final int version;
  final bool isDeleted;
  final DateTime updatedAt;
  const CachedResource({
    required this.subject,
    required this.tenantId,
    required this.entityType,
    required this.entityId,
    required this.payloadJson,
    required this.version,
    required this.isDeleted,
    required this.updatedAt,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['subject'] = Variable<String>(subject);
    map['tenant_id'] = Variable<String>(tenantId);
    map['entity_type'] = Variable<String>(entityType);
    map['entity_id'] = Variable<String>(entityId);
    map['payload_json'] = Variable<String>(payloadJson);
    map['version'] = Variable<int>(version);
    map['is_deleted'] = Variable<bool>(isDeleted);
    map['updated_at'] = Variable<DateTime>(updatedAt);
    return map;
  }

  CachedResourcesCompanion toCompanion(bool nullToAbsent) {
    return CachedResourcesCompanion(
      subject: Value(subject),
      tenantId: Value(tenantId),
      entityType: Value(entityType),
      entityId: Value(entityId),
      payloadJson: Value(payloadJson),
      version: Value(version),
      isDeleted: Value(isDeleted),
      updatedAt: Value(updatedAt),
    );
  }

  factory CachedResource.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return CachedResource(
      subject: serializer.fromJson<String>(json['subject']),
      tenantId: serializer.fromJson<String>(json['tenantId']),
      entityType: serializer.fromJson<String>(json['entityType']),
      entityId: serializer.fromJson<String>(json['entityId']),
      payloadJson: serializer.fromJson<String>(json['payloadJson']),
      version: serializer.fromJson<int>(json['version']),
      isDeleted: serializer.fromJson<bool>(json['isDeleted']),
      updatedAt: serializer.fromJson<DateTime>(json['updatedAt']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'subject': serializer.toJson<String>(subject),
      'tenantId': serializer.toJson<String>(tenantId),
      'entityType': serializer.toJson<String>(entityType),
      'entityId': serializer.toJson<String>(entityId),
      'payloadJson': serializer.toJson<String>(payloadJson),
      'version': serializer.toJson<int>(version),
      'isDeleted': serializer.toJson<bool>(isDeleted),
      'updatedAt': serializer.toJson<DateTime>(updatedAt),
    };
  }

  CachedResource copyWith({
    String? subject,
    String? tenantId,
    String? entityType,
    String? entityId,
    String? payloadJson,
    int? version,
    bool? isDeleted,
    DateTime? updatedAt,
  }) => CachedResource(
    subject: subject ?? this.subject,
    tenantId: tenantId ?? this.tenantId,
    entityType: entityType ?? this.entityType,
    entityId: entityId ?? this.entityId,
    payloadJson: payloadJson ?? this.payloadJson,
    version: version ?? this.version,
    isDeleted: isDeleted ?? this.isDeleted,
    updatedAt: updatedAt ?? this.updatedAt,
  );
  CachedResource copyWithCompanion(CachedResourcesCompanion data) {
    return CachedResource(
      subject: data.subject.present ? data.subject.value : this.subject,
      tenantId: data.tenantId.present ? data.tenantId.value : this.tenantId,
      entityType: data.entityType.present
          ? data.entityType.value
          : this.entityType,
      entityId: data.entityId.present ? data.entityId.value : this.entityId,
      payloadJson: data.payloadJson.present
          ? data.payloadJson.value
          : this.payloadJson,
      version: data.version.present ? data.version.value : this.version,
      isDeleted: data.isDeleted.present ? data.isDeleted.value : this.isDeleted,
      updatedAt: data.updatedAt.present ? data.updatedAt.value : this.updatedAt,
    );
  }

  @override
  String toString() {
    return (StringBuffer('CachedResource(')
          ..write('subject: $subject, ')
          ..write('tenantId: $tenantId, ')
          ..write('entityType: $entityType, ')
          ..write('entityId: $entityId, ')
          ..write('payloadJson: $payloadJson, ')
          ..write('version: $version, ')
          ..write('isDeleted: $isDeleted, ')
          ..write('updatedAt: $updatedAt')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(
    subject,
    tenantId,
    entityType,
    entityId,
    payloadJson,
    version,
    isDeleted,
    updatedAt,
  );
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is CachedResource &&
          other.subject == this.subject &&
          other.tenantId == this.tenantId &&
          other.entityType == this.entityType &&
          other.entityId == this.entityId &&
          other.payloadJson == this.payloadJson &&
          other.version == this.version &&
          other.isDeleted == this.isDeleted &&
          other.updatedAt == this.updatedAt);
}

class CachedResourcesCompanion extends UpdateCompanion<CachedResource> {
  final Value<String> subject;
  final Value<String> tenantId;
  final Value<String> entityType;
  final Value<String> entityId;
  final Value<String> payloadJson;
  final Value<int> version;
  final Value<bool> isDeleted;
  final Value<DateTime> updatedAt;
  final Value<int> rowid;
  const CachedResourcesCompanion({
    this.subject = const Value.absent(),
    this.tenantId = const Value.absent(),
    this.entityType = const Value.absent(),
    this.entityId = const Value.absent(),
    this.payloadJson = const Value.absent(),
    this.version = const Value.absent(),
    this.isDeleted = const Value.absent(),
    this.updatedAt = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  CachedResourcesCompanion.insert({
    required String subject,
    required String tenantId,
    required String entityType,
    required String entityId,
    required String payloadJson,
    this.version = const Value.absent(),
    this.isDeleted = const Value.absent(),
    required DateTime updatedAt,
    this.rowid = const Value.absent(),
  }) : subject = Value(subject),
       tenantId = Value(tenantId),
       entityType = Value(entityType),
       entityId = Value(entityId),
       payloadJson = Value(payloadJson),
       updatedAt = Value(updatedAt);
  static Insertable<CachedResource> custom({
    Expression<String>? subject,
    Expression<String>? tenantId,
    Expression<String>? entityType,
    Expression<String>? entityId,
    Expression<String>? payloadJson,
    Expression<int>? version,
    Expression<bool>? isDeleted,
    Expression<DateTime>? updatedAt,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (subject != null) 'subject': subject,
      if (tenantId != null) 'tenant_id': tenantId,
      if (entityType != null) 'entity_type': entityType,
      if (entityId != null) 'entity_id': entityId,
      if (payloadJson != null) 'payload_json': payloadJson,
      if (version != null) 'version': version,
      if (isDeleted != null) 'is_deleted': isDeleted,
      if (updatedAt != null) 'updated_at': updatedAt,
      if (rowid != null) 'rowid': rowid,
    });
  }

  CachedResourcesCompanion copyWith({
    Value<String>? subject,
    Value<String>? tenantId,
    Value<String>? entityType,
    Value<String>? entityId,
    Value<String>? payloadJson,
    Value<int>? version,
    Value<bool>? isDeleted,
    Value<DateTime>? updatedAt,
    Value<int>? rowid,
  }) {
    return CachedResourcesCompanion(
      subject: subject ?? this.subject,
      tenantId: tenantId ?? this.tenantId,
      entityType: entityType ?? this.entityType,
      entityId: entityId ?? this.entityId,
      payloadJson: payloadJson ?? this.payloadJson,
      version: version ?? this.version,
      isDeleted: isDeleted ?? this.isDeleted,
      updatedAt: updatedAt ?? this.updatedAt,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (subject.present) {
      map['subject'] = Variable<String>(subject.value);
    }
    if (tenantId.present) {
      map['tenant_id'] = Variable<String>(tenantId.value);
    }
    if (entityType.present) {
      map['entity_type'] = Variable<String>(entityType.value);
    }
    if (entityId.present) {
      map['entity_id'] = Variable<String>(entityId.value);
    }
    if (payloadJson.present) {
      map['payload_json'] = Variable<String>(payloadJson.value);
    }
    if (version.present) {
      map['version'] = Variable<int>(version.value);
    }
    if (isDeleted.present) {
      map['is_deleted'] = Variable<bool>(isDeleted.value);
    }
    if (updatedAt.present) {
      map['updated_at'] = Variable<DateTime>(updatedAt.value);
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('CachedResourcesCompanion(')
          ..write('subject: $subject, ')
          ..write('tenantId: $tenantId, ')
          ..write('entityType: $entityType, ')
          ..write('entityId: $entityId, ')
          ..write('payloadJson: $payloadJson, ')
          ..write('version: $version, ')
          ..write('isDeleted: $isDeleted, ')
          ..write('updatedAt: $updatedAt, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

class $PendingSyncOperationsTable extends PendingSyncOperations
    with TableInfo<$PendingSyncOperationsTable, PendingSyncOperation> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $PendingSyncOperationsTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _operationIdMeta = const VerificationMeta(
    'operationId',
  );
  @override
  late final GeneratedColumn<String> operationId = GeneratedColumn<String>(
    'operation_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _subjectMeta = const VerificationMeta(
    'subject',
  );
  @override
  late final GeneratedColumn<String> subject = GeneratedColumn<String>(
    'subject',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _tenantIdMeta = const VerificationMeta(
    'tenantId',
  );
  @override
  late final GeneratedColumn<String> tenantId = GeneratedColumn<String>(
    'tenant_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _deviceIdMeta = const VerificationMeta(
    'deviceId',
  );
  @override
  late final GeneratedColumn<String> deviceId = GeneratedColumn<String>(
    'device_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _entityIdMeta = const VerificationMeta(
    'entityId',
  );
  @override
  late final GeneratedColumn<String> entityId = GeneratedColumn<String>(
    'entity_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _entityTypeMeta = const VerificationMeta(
    'entityType',
  );
  @override
  late final GeneratedColumn<String> entityType = GeneratedColumn<String>(
    'entity_type',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _operationTypeMeta = const VerificationMeta(
    'operationType',
  );
  @override
  late final GeneratedColumn<String> operationType = GeneratedColumn<String>(
    'operation_type',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _endpointMeta = const VerificationMeta(
    'endpoint',
  );
  @override
  late final GeneratedColumn<String> endpoint = GeneratedColumn<String>(
    'endpoint',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _payloadJsonMeta = const VerificationMeta(
    'payloadJson',
  );
  @override
  late final GeneratedColumn<String> payloadJson = GeneratedColumn<String>(
    'payload_json',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _versionMeta = const VerificationMeta(
    'version',
  );
  @override
  late final GeneratedColumn<int> version = GeneratedColumn<int>(
    'version',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: false,
    defaultValue: const Constant(0),
  );
  static const VerificationMeta _statusMeta = const VerificationMeta('status');
  @override
  late final GeneratedColumn<String> status = GeneratedColumn<String>(
    'status',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
    defaultValue: const Constant('pending'),
  );
  static const VerificationMeta _retryCountMeta = const VerificationMeta(
    'retryCount',
  );
  @override
  late final GeneratedColumn<int> retryCount = GeneratedColumn<int>(
    'retry_count',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: false,
    defaultValue: const Constant(0),
  );
  static const VerificationMeta _lastErrorMeta = const VerificationMeta(
    'lastError',
  );
  @override
  late final GeneratedColumn<String> lastError = GeneratedColumn<String>(
    'last_error',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _clientTimestampMeta = const VerificationMeta(
    'clientTimestamp',
  );
  @override
  late final GeneratedColumn<DateTime> clientTimestamp =
      GeneratedColumn<DateTime>(
        'client_timestamp',
        aliasedName,
        false,
        type: DriftSqlType.dateTime,
        requiredDuringInsert: true,
      );
  static const VerificationMeta _nextAttemptAtMeta = const VerificationMeta(
    'nextAttemptAt',
  );
  @override
  late final GeneratedColumn<DateTime> nextAttemptAt =
      GeneratedColumn<DateTime>(
        'next_attempt_at',
        aliasedName,
        true,
        type: DriftSqlType.dateTime,
        requiredDuringInsert: false,
      );
  @override
  List<GeneratedColumn> get $columns => [
    operationId,
    subject,
    tenantId,
    deviceId,
    entityId,
    entityType,
    operationType,
    endpoint,
    payloadJson,
    version,
    status,
    retryCount,
    lastError,
    clientTimestamp,
    nextAttemptAt,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'pending_sync_operations';
  @override
  VerificationContext validateIntegrity(
    Insertable<PendingSyncOperation> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('operation_id')) {
      context.handle(
        _operationIdMeta,
        operationId.isAcceptableOrUnknown(
          data['operation_id']!,
          _operationIdMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_operationIdMeta);
    }
    if (data.containsKey('subject')) {
      context.handle(
        _subjectMeta,
        subject.isAcceptableOrUnknown(data['subject']!, _subjectMeta),
      );
    } else if (isInserting) {
      context.missing(_subjectMeta);
    }
    if (data.containsKey('tenant_id')) {
      context.handle(
        _tenantIdMeta,
        tenantId.isAcceptableOrUnknown(data['tenant_id']!, _tenantIdMeta),
      );
    } else if (isInserting) {
      context.missing(_tenantIdMeta);
    }
    if (data.containsKey('device_id')) {
      context.handle(
        _deviceIdMeta,
        deviceId.isAcceptableOrUnknown(data['device_id']!, _deviceIdMeta),
      );
    } else if (isInserting) {
      context.missing(_deviceIdMeta);
    }
    if (data.containsKey('entity_id')) {
      context.handle(
        _entityIdMeta,
        entityId.isAcceptableOrUnknown(data['entity_id']!, _entityIdMeta),
      );
    } else if (isInserting) {
      context.missing(_entityIdMeta);
    }
    if (data.containsKey('entity_type')) {
      context.handle(
        _entityTypeMeta,
        entityType.isAcceptableOrUnknown(data['entity_type']!, _entityTypeMeta),
      );
    } else if (isInserting) {
      context.missing(_entityTypeMeta);
    }
    if (data.containsKey('operation_type')) {
      context.handle(
        _operationTypeMeta,
        operationType.isAcceptableOrUnknown(
          data['operation_type']!,
          _operationTypeMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_operationTypeMeta);
    }
    if (data.containsKey('endpoint')) {
      context.handle(
        _endpointMeta,
        endpoint.isAcceptableOrUnknown(data['endpoint']!, _endpointMeta),
      );
    } else if (isInserting) {
      context.missing(_endpointMeta);
    }
    if (data.containsKey('payload_json')) {
      context.handle(
        _payloadJsonMeta,
        payloadJson.isAcceptableOrUnknown(
          data['payload_json']!,
          _payloadJsonMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_payloadJsonMeta);
    }
    if (data.containsKey('version')) {
      context.handle(
        _versionMeta,
        version.isAcceptableOrUnknown(data['version']!, _versionMeta),
      );
    }
    if (data.containsKey('status')) {
      context.handle(
        _statusMeta,
        status.isAcceptableOrUnknown(data['status']!, _statusMeta),
      );
    }
    if (data.containsKey('retry_count')) {
      context.handle(
        _retryCountMeta,
        retryCount.isAcceptableOrUnknown(data['retry_count']!, _retryCountMeta),
      );
    }
    if (data.containsKey('last_error')) {
      context.handle(
        _lastErrorMeta,
        lastError.isAcceptableOrUnknown(data['last_error']!, _lastErrorMeta),
      );
    }
    if (data.containsKey('client_timestamp')) {
      context.handle(
        _clientTimestampMeta,
        clientTimestamp.isAcceptableOrUnknown(
          data['client_timestamp']!,
          _clientTimestampMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_clientTimestampMeta);
    }
    if (data.containsKey('next_attempt_at')) {
      context.handle(
        _nextAttemptAtMeta,
        nextAttemptAt.isAcceptableOrUnknown(
          data['next_attempt_at']!,
          _nextAttemptAtMeta,
        ),
      );
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {operationId};
  @override
  PendingSyncOperation map(Map<String, dynamic> data, {String? tablePrefix}) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return PendingSyncOperation(
      operationId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}operation_id'],
      )!,
      subject: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}subject'],
      )!,
      tenantId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}tenant_id'],
      )!,
      deviceId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}device_id'],
      )!,
      entityId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}entity_id'],
      )!,
      entityType: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}entity_type'],
      )!,
      operationType: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}operation_type'],
      )!,
      endpoint: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}endpoint'],
      )!,
      payloadJson: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}payload_json'],
      )!,
      version: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}version'],
      )!,
      status: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}status'],
      )!,
      retryCount: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}retry_count'],
      )!,
      lastError: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_error'],
      ),
      clientTimestamp: attachedDatabase.typeMapping.read(
        DriftSqlType.dateTime,
        data['${effectivePrefix}client_timestamp'],
      )!,
      nextAttemptAt: attachedDatabase.typeMapping.read(
        DriftSqlType.dateTime,
        data['${effectivePrefix}next_attempt_at'],
      ),
    );
  }

  @override
  $PendingSyncOperationsTable createAlias(String alias) {
    return $PendingSyncOperationsTable(attachedDatabase, alias);
  }
}

class PendingSyncOperation extends DataClass
    implements Insertable<PendingSyncOperation> {
  final String operationId;
  final String subject;
  final String tenantId;
  final String deviceId;
  final String entityId;
  final String entityType;
  final String operationType;
  final String endpoint;
  final String payloadJson;
  final int version;
  final String status;
  final int retryCount;
  final String? lastError;
  final DateTime clientTimestamp;
  final DateTime? nextAttemptAt;
  const PendingSyncOperation({
    required this.operationId,
    required this.subject,
    required this.tenantId,
    required this.deviceId,
    required this.entityId,
    required this.entityType,
    required this.operationType,
    required this.endpoint,
    required this.payloadJson,
    required this.version,
    required this.status,
    required this.retryCount,
    this.lastError,
    required this.clientTimestamp,
    this.nextAttemptAt,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['operation_id'] = Variable<String>(operationId);
    map['subject'] = Variable<String>(subject);
    map['tenant_id'] = Variable<String>(tenantId);
    map['device_id'] = Variable<String>(deviceId);
    map['entity_id'] = Variable<String>(entityId);
    map['entity_type'] = Variable<String>(entityType);
    map['operation_type'] = Variable<String>(operationType);
    map['endpoint'] = Variable<String>(endpoint);
    map['payload_json'] = Variable<String>(payloadJson);
    map['version'] = Variable<int>(version);
    map['status'] = Variable<String>(status);
    map['retry_count'] = Variable<int>(retryCount);
    if (!nullToAbsent || lastError != null) {
      map['last_error'] = Variable<String>(lastError);
    }
    map['client_timestamp'] = Variable<DateTime>(clientTimestamp);
    if (!nullToAbsent || nextAttemptAt != null) {
      map['next_attempt_at'] = Variable<DateTime>(nextAttemptAt);
    }
    return map;
  }

  PendingSyncOperationsCompanion toCompanion(bool nullToAbsent) {
    return PendingSyncOperationsCompanion(
      operationId: Value(operationId),
      subject: Value(subject),
      tenantId: Value(tenantId),
      deviceId: Value(deviceId),
      entityId: Value(entityId),
      entityType: Value(entityType),
      operationType: Value(operationType),
      endpoint: Value(endpoint),
      payloadJson: Value(payloadJson),
      version: Value(version),
      status: Value(status),
      retryCount: Value(retryCount),
      lastError: lastError == null && nullToAbsent
          ? const Value.absent()
          : Value(lastError),
      clientTimestamp: Value(clientTimestamp),
      nextAttemptAt: nextAttemptAt == null && nullToAbsent
          ? const Value.absent()
          : Value(nextAttemptAt),
    );
  }

  factory PendingSyncOperation.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return PendingSyncOperation(
      operationId: serializer.fromJson<String>(json['operationId']),
      subject: serializer.fromJson<String>(json['subject']),
      tenantId: serializer.fromJson<String>(json['tenantId']),
      deviceId: serializer.fromJson<String>(json['deviceId']),
      entityId: serializer.fromJson<String>(json['entityId']),
      entityType: serializer.fromJson<String>(json['entityType']),
      operationType: serializer.fromJson<String>(json['operationType']),
      endpoint: serializer.fromJson<String>(json['endpoint']),
      payloadJson: serializer.fromJson<String>(json['payloadJson']),
      version: serializer.fromJson<int>(json['version']),
      status: serializer.fromJson<String>(json['status']),
      retryCount: serializer.fromJson<int>(json['retryCount']),
      lastError: serializer.fromJson<String?>(json['lastError']),
      clientTimestamp: serializer.fromJson<DateTime>(json['clientTimestamp']),
      nextAttemptAt: serializer.fromJson<DateTime?>(json['nextAttemptAt']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'operationId': serializer.toJson<String>(operationId),
      'subject': serializer.toJson<String>(subject),
      'tenantId': serializer.toJson<String>(tenantId),
      'deviceId': serializer.toJson<String>(deviceId),
      'entityId': serializer.toJson<String>(entityId),
      'entityType': serializer.toJson<String>(entityType),
      'operationType': serializer.toJson<String>(operationType),
      'endpoint': serializer.toJson<String>(endpoint),
      'payloadJson': serializer.toJson<String>(payloadJson),
      'version': serializer.toJson<int>(version),
      'status': serializer.toJson<String>(status),
      'retryCount': serializer.toJson<int>(retryCount),
      'lastError': serializer.toJson<String?>(lastError),
      'clientTimestamp': serializer.toJson<DateTime>(clientTimestamp),
      'nextAttemptAt': serializer.toJson<DateTime?>(nextAttemptAt),
    };
  }

  PendingSyncOperation copyWith({
    String? operationId,
    String? subject,
    String? tenantId,
    String? deviceId,
    String? entityId,
    String? entityType,
    String? operationType,
    String? endpoint,
    String? payloadJson,
    int? version,
    String? status,
    int? retryCount,
    Value<String?> lastError = const Value.absent(),
    DateTime? clientTimestamp,
    Value<DateTime?> nextAttemptAt = const Value.absent(),
  }) => PendingSyncOperation(
    operationId: operationId ?? this.operationId,
    subject: subject ?? this.subject,
    tenantId: tenantId ?? this.tenantId,
    deviceId: deviceId ?? this.deviceId,
    entityId: entityId ?? this.entityId,
    entityType: entityType ?? this.entityType,
    operationType: operationType ?? this.operationType,
    endpoint: endpoint ?? this.endpoint,
    payloadJson: payloadJson ?? this.payloadJson,
    version: version ?? this.version,
    status: status ?? this.status,
    retryCount: retryCount ?? this.retryCount,
    lastError: lastError.present ? lastError.value : this.lastError,
    clientTimestamp: clientTimestamp ?? this.clientTimestamp,
    nextAttemptAt: nextAttemptAt.present
        ? nextAttemptAt.value
        : this.nextAttemptAt,
  );
  PendingSyncOperation copyWithCompanion(PendingSyncOperationsCompanion data) {
    return PendingSyncOperation(
      operationId: data.operationId.present
          ? data.operationId.value
          : this.operationId,
      subject: data.subject.present ? data.subject.value : this.subject,
      tenantId: data.tenantId.present ? data.tenantId.value : this.tenantId,
      deviceId: data.deviceId.present ? data.deviceId.value : this.deviceId,
      entityId: data.entityId.present ? data.entityId.value : this.entityId,
      entityType: data.entityType.present
          ? data.entityType.value
          : this.entityType,
      operationType: data.operationType.present
          ? data.operationType.value
          : this.operationType,
      endpoint: data.endpoint.present ? data.endpoint.value : this.endpoint,
      payloadJson: data.payloadJson.present
          ? data.payloadJson.value
          : this.payloadJson,
      version: data.version.present ? data.version.value : this.version,
      status: data.status.present ? data.status.value : this.status,
      retryCount: data.retryCount.present
          ? data.retryCount.value
          : this.retryCount,
      lastError: data.lastError.present ? data.lastError.value : this.lastError,
      clientTimestamp: data.clientTimestamp.present
          ? data.clientTimestamp.value
          : this.clientTimestamp,
      nextAttemptAt: data.nextAttemptAt.present
          ? data.nextAttemptAt.value
          : this.nextAttemptAt,
    );
  }

  @override
  String toString() {
    return (StringBuffer('PendingSyncOperation(')
          ..write('operationId: $operationId, ')
          ..write('subject: $subject, ')
          ..write('tenantId: $tenantId, ')
          ..write('deviceId: $deviceId, ')
          ..write('entityId: $entityId, ')
          ..write('entityType: $entityType, ')
          ..write('operationType: $operationType, ')
          ..write('endpoint: $endpoint, ')
          ..write('payloadJson: $payloadJson, ')
          ..write('version: $version, ')
          ..write('status: $status, ')
          ..write('retryCount: $retryCount, ')
          ..write('lastError: $lastError, ')
          ..write('clientTimestamp: $clientTimestamp, ')
          ..write('nextAttemptAt: $nextAttemptAt')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(
    operationId,
    subject,
    tenantId,
    deviceId,
    entityId,
    entityType,
    operationType,
    endpoint,
    payloadJson,
    version,
    status,
    retryCount,
    lastError,
    clientTimestamp,
    nextAttemptAt,
  );
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is PendingSyncOperation &&
          other.operationId == this.operationId &&
          other.subject == this.subject &&
          other.tenantId == this.tenantId &&
          other.deviceId == this.deviceId &&
          other.entityId == this.entityId &&
          other.entityType == this.entityType &&
          other.operationType == this.operationType &&
          other.endpoint == this.endpoint &&
          other.payloadJson == this.payloadJson &&
          other.version == this.version &&
          other.status == this.status &&
          other.retryCount == this.retryCount &&
          other.lastError == this.lastError &&
          other.clientTimestamp == this.clientTimestamp &&
          other.nextAttemptAt == this.nextAttemptAt);
}

class PendingSyncOperationsCompanion
    extends UpdateCompanion<PendingSyncOperation> {
  final Value<String> operationId;
  final Value<String> subject;
  final Value<String> tenantId;
  final Value<String> deviceId;
  final Value<String> entityId;
  final Value<String> entityType;
  final Value<String> operationType;
  final Value<String> endpoint;
  final Value<String> payloadJson;
  final Value<int> version;
  final Value<String> status;
  final Value<int> retryCount;
  final Value<String?> lastError;
  final Value<DateTime> clientTimestamp;
  final Value<DateTime?> nextAttemptAt;
  final Value<int> rowid;
  const PendingSyncOperationsCompanion({
    this.operationId = const Value.absent(),
    this.subject = const Value.absent(),
    this.tenantId = const Value.absent(),
    this.deviceId = const Value.absent(),
    this.entityId = const Value.absent(),
    this.entityType = const Value.absent(),
    this.operationType = const Value.absent(),
    this.endpoint = const Value.absent(),
    this.payloadJson = const Value.absent(),
    this.version = const Value.absent(),
    this.status = const Value.absent(),
    this.retryCount = const Value.absent(),
    this.lastError = const Value.absent(),
    this.clientTimestamp = const Value.absent(),
    this.nextAttemptAt = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  PendingSyncOperationsCompanion.insert({
    required String operationId,
    required String subject,
    required String tenantId,
    required String deviceId,
    required String entityId,
    required String entityType,
    required String operationType,
    required String endpoint,
    required String payloadJson,
    this.version = const Value.absent(),
    this.status = const Value.absent(),
    this.retryCount = const Value.absent(),
    this.lastError = const Value.absent(),
    required DateTime clientTimestamp,
    this.nextAttemptAt = const Value.absent(),
    this.rowid = const Value.absent(),
  }) : operationId = Value(operationId),
       subject = Value(subject),
       tenantId = Value(tenantId),
       deviceId = Value(deviceId),
       entityId = Value(entityId),
       entityType = Value(entityType),
       operationType = Value(operationType),
       endpoint = Value(endpoint),
       payloadJson = Value(payloadJson),
       clientTimestamp = Value(clientTimestamp);
  static Insertable<PendingSyncOperation> custom({
    Expression<String>? operationId,
    Expression<String>? subject,
    Expression<String>? tenantId,
    Expression<String>? deviceId,
    Expression<String>? entityId,
    Expression<String>? entityType,
    Expression<String>? operationType,
    Expression<String>? endpoint,
    Expression<String>? payloadJson,
    Expression<int>? version,
    Expression<String>? status,
    Expression<int>? retryCount,
    Expression<String>? lastError,
    Expression<DateTime>? clientTimestamp,
    Expression<DateTime>? nextAttemptAt,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (operationId != null) 'operation_id': operationId,
      if (subject != null) 'subject': subject,
      if (tenantId != null) 'tenant_id': tenantId,
      if (deviceId != null) 'device_id': deviceId,
      if (entityId != null) 'entity_id': entityId,
      if (entityType != null) 'entity_type': entityType,
      if (operationType != null) 'operation_type': operationType,
      if (endpoint != null) 'endpoint': endpoint,
      if (payloadJson != null) 'payload_json': payloadJson,
      if (version != null) 'version': version,
      if (status != null) 'status': status,
      if (retryCount != null) 'retry_count': retryCount,
      if (lastError != null) 'last_error': lastError,
      if (clientTimestamp != null) 'client_timestamp': clientTimestamp,
      if (nextAttemptAt != null) 'next_attempt_at': nextAttemptAt,
      if (rowid != null) 'rowid': rowid,
    });
  }

  PendingSyncOperationsCompanion copyWith({
    Value<String>? operationId,
    Value<String>? subject,
    Value<String>? tenantId,
    Value<String>? deviceId,
    Value<String>? entityId,
    Value<String>? entityType,
    Value<String>? operationType,
    Value<String>? endpoint,
    Value<String>? payloadJson,
    Value<int>? version,
    Value<String>? status,
    Value<int>? retryCount,
    Value<String?>? lastError,
    Value<DateTime>? clientTimestamp,
    Value<DateTime?>? nextAttemptAt,
    Value<int>? rowid,
  }) {
    return PendingSyncOperationsCompanion(
      operationId: operationId ?? this.operationId,
      subject: subject ?? this.subject,
      tenantId: tenantId ?? this.tenantId,
      deviceId: deviceId ?? this.deviceId,
      entityId: entityId ?? this.entityId,
      entityType: entityType ?? this.entityType,
      operationType: operationType ?? this.operationType,
      endpoint: endpoint ?? this.endpoint,
      payloadJson: payloadJson ?? this.payloadJson,
      version: version ?? this.version,
      status: status ?? this.status,
      retryCount: retryCount ?? this.retryCount,
      lastError: lastError ?? this.lastError,
      clientTimestamp: clientTimestamp ?? this.clientTimestamp,
      nextAttemptAt: nextAttemptAt ?? this.nextAttemptAt,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (operationId.present) {
      map['operation_id'] = Variable<String>(operationId.value);
    }
    if (subject.present) {
      map['subject'] = Variable<String>(subject.value);
    }
    if (tenantId.present) {
      map['tenant_id'] = Variable<String>(tenantId.value);
    }
    if (deviceId.present) {
      map['device_id'] = Variable<String>(deviceId.value);
    }
    if (entityId.present) {
      map['entity_id'] = Variable<String>(entityId.value);
    }
    if (entityType.present) {
      map['entity_type'] = Variable<String>(entityType.value);
    }
    if (operationType.present) {
      map['operation_type'] = Variable<String>(operationType.value);
    }
    if (endpoint.present) {
      map['endpoint'] = Variable<String>(endpoint.value);
    }
    if (payloadJson.present) {
      map['payload_json'] = Variable<String>(payloadJson.value);
    }
    if (version.present) {
      map['version'] = Variable<int>(version.value);
    }
    if (status.present) {
      map['status'] = Variable<String>(status.value);
    }
    if (retryCount.present) {
      map['retry_count'] = Variable<int>(retryCount.value);
    }
    if (lastError.present) {
      map['last_error'] = Variable<String>(lastError.value);
    }
    if (clientTimestamp.present) {
      map['client_timestamp'] = Variable<DateTime>(clientTimestamp.value);
    }
    if (nextAttemptAt.present) {
      map['next_attempt_at'] = Variable<DateTime>(nextAttemptAt.value);
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('PendingSyncOperationsCompanion(')
          ..write('operationId: $operationId, ')
          ..write('subject: $subject, ')
          ..write('tenantId: $tenantId, ')
          ..write('deviceId: $deviceId, ')
          ..write('entityId: $entityId, ')
          ..write('entityType: $entityType, ')
          ..write('operationType: $operationType, ')
          ..write('endpoint: $endpoint, ')
          ..write('payloadJson: $payloadJson, ')
          ..write('version: $version, ')
          ..write('status: $status, ')
          ..write('retryCount: $retryCount, ')
          ..write('lastError: $lastError, ')
          ..write('clientTimestamp: $clientTimestamp, ')
          ..write('nextAttemptAt: $nextAttemptAt, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

class $SyncInboxTable extends SyncInbox
    with TableInfo<$SyncInboxTable, SyncInboxData> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $SyncInboxTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _operationIdMeta = const VerificationMeta(
    'operationId',
  );
  @override
  late final GeneratedColumn<String> operationId = GeneratedColumn<String>(
    'operation_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _subjectMeta = const VerificationMeta(
    'subject',
  );
  @override
  late final GeneratedColumn<String> subject = GeneratedColumn<String>(
    'subject',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _tenantIdMeta = const VerificationMeta(
    'tenantId',
  );
  @override
  late final GeneratedColumn<String> tenantId = GeneratedColumn<String>(
    'tenant_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _receivedAtMeta = const VerificationMeta(
    'receivedAt',
  );
  @override
  late final GeneratedColumn<DateTime> receivedAt = GeneratedColumn<DateTime>(
    'received_at',
    aliasedName,
    false,
    type: DriftSqlType.dateTime,
    requiredDuringInsert: true,
  );
  @override
  List<GeneratedColumn> get $columns => [
    operationId,
    subject,
    tenantId,
    receivedAt,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'sync_inbox';
  @override
  VerificationContext validateIntegrity(
    Insertable<SyncInboxData> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('operation_id')) {
      context.handle(
        _operationIdMeta,
        operationId.isAcceptableOrUnknown(
          data['operation_id']!,
          _operationIdMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_operationIdMeta);
    }
    if (data.containsKey('subject')) {
      context.handle(
        _subjectMeta,
        subject.isAcceptableOrUnknown(data['subject']!, _subjectMeta),
      );
    } else if (isInserting) {
      context.missing(_subjectMeta);
    }
    if (data.containsKey('tenant_id')) {
      context.handle(
        _tenantIdMeta,
        tenantId.isAcceptableOrUnknown(data['tenant_id']!, _tenantIdMeta),
      );
    } else if (isInserting) {
      context.missing(_tenantIdMeta);
    }
    if (data.containsKey('received_at')) {
      context.handle(
        _receivedAtMeta,
        receivedAt.isAcceptableOrUnknown(data['received_at']!, _receivedAtMeta),
      );
    } else if (isInserting) {
      context.missing(_receivedAtMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {operationId};
  @override
  SyncInboxData map(Map<String, dynamic> data, {String? tablePrefix}) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return SyncInboxData(
      operationId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}operation_id'],
      )!,
      subject: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}subject'],
      )!,
      tenantId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}tenant_id'],
      )!,
      receivedAt: attachedDatabase.typeMapping.read(
        DriftSqlType.dateTime,
        data['${effectivePrefix}received_at'],
      )!,
    );
  }

  @override
  $SyncInboxTable createAlias(String alias) {
    return $SyncInboxTable(attachedDatabase, alias);
  }
}

class SyncInboxData extends DataClass implements Insertable<SyncInboxData> {
  final String operationId;
  final String subject;
  final String tenantId;
  final DateTime receivedAt;
  const SyncInboxData({
    required this.operationId,
    required this.subject,
    required this.tenantId,
    required this.receivedAt,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['operation_id'] = Variable<String>(operationId);
    map['subject'] = Variable<String>(subject);
    map['tenant_id'] = Variable<String>(tenantId);
    map['received_at'] = Variable<DateTime>(receivedAt);
    return map;
  }

  SyncInboxCompanion toCompanion(bool nullToAbsent) {
    return SyncInboxCompanion(
      operationId: Value(operationId),
      subject: Value(subject),
      tenantId: Value(tenantId),
      receivedAt: Value(receivedAt),
    );
  }

  factory SyncInboxData.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return SyncInboxData(
      operationId: serializer.fromJson<String>(json['operationId']),
      subject: serializer.fromJson<String>(json['subject']),
      tenantId: serializer.fromJson<String>(json['tenantId']),
      receivedAt: serializer.fromJson<DateTime>(json['receivedAt']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'operationId': serializer.toJson<String>(operationId),
      'subject': serializer.toJson<String>(subject),
      'tenantId': serializer.toJson<String>(tenantId),
      'receivedAt': serializer.toJson<DateTime>(receivedAt),
    };
  }

  SyncInboxData copyWith({
    String? operationId,
    String? subject,
    String? tenantId,
    DateTime? receivedAt,
  }) => SyncInboxData(
    operationId: operationId ?? this.operationId,
    subject: subject ?? this.subject,
    tenantId: tenantId ?? this.tenantId,
    receivedAt: receivedAt ?? this.receivedAt,
  );
  SyncInboxData copyWithCompanion(SyncInboxCompanion data) {
    return SyncInboxData(
      operationId: data.operationId.present
          ? data.operationId.value
          : this.operationId,
      subject: data.subject.present ? data.subject.value : this.subject,
      tenantId: data.tenantId.present ? data.tenantId.value : this.tenantId,
      receivedAt: data.receivedAt.present
          ? data.receivedAt.value
          : this.receivedAt,
    );
  }

  @override
  String toString() {
    return (StringBuffer('SyncInboxData(')
          ..write('operationId: $operationId, ')
          ..write('subject: $subject, ')
          ..write('tenantId: $tenantId, ')
          ..write('receivedAt: $receivedAt')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(operationId, subject, tenantId, receivedAt);
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is SyncInboxData &&
          other.operationId == this.operationId &&
          other.subject == this.subject &&
          other.tenantId == this.tenantId &&
          other.receivedAt == this.receivedAt);
}

class SyncInboxCompanion extends UpdateCompanion<SyncInboxData> {
  final Value<String> operationId;
  final Value<String> subject;
  final Value<String> tenantId;
  final Value<DateTime> receivedAt;
  final Value<int> rowid;
  const SyncInboxCompanion({
    this.operationId = const Value.absent(),
    this.subject = const Value.absent(),
    this.tenantId = const Value.absent(),
    this.receivedAt = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  SyncInboxCompanion.insert({
    required String operationId,
    required String subject,
    required String tenantId,
    required DateTime receivedAt,
    this.rowid = const Value.absent(),
  }) : operationId = Value(operationId),
       subject = Value(subject),
       tenantId = Value(tenantId),
       receivedAt = Value(receivedAt);
  static Insertable<SyncInboxData> custom({
    Expression<String>? operationId,
    Expression<String>? subject,
    Expression<String>? tenantId,
    Expression<DateTime>? receivedAt,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (operationId != null) 'operation_id': operationId,
      if (subject != null) 'subject': subject,
      if (tenantId != null) 'tenant_id': tenantId,
      if (receivedAt != null) 'received_at': receivedAt,
      if (rowid != null) 'rowid': rowid,
    });
  }

  SyncInboxCompanion copyWith({
    Value<String>? operationId,
    Value<String>? subject,
    Value<String>? tenantId,
    Value<DateTime>? receivedAt,
    Value<int>? rowid,
  }) {
    return SyncInboxCompanion(
      operationId: operationId ?? this.operationId,
      subject: subject ?? this.subject,
      tenantId: tenantId ?? this.tenantId,
      receivedAt: receivedAt ?? this.receivedAt,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (operationId.present) {
      map['operation_id'] = Variable<String>(operationId.value);
    }
    if (subject.present) {
      map['subject'] = Variable<String>(subject.value);
    }
    if (tenantId.present) {
      map['tenant_id'] = Variable<String>(tenantId.value);
    }
    if (receivedAt.present) {
      map['received_at'] = Variable<DateTime>(receivedAt.value);
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('SyncInboxCompanion(')
          ..write('operationId: $operationId, ')
          ..write('subject: $subject, ')
          ..write('tenantId: $tenantId, ')
          ..write('receivedAt: $receivedAt, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

class $SyncStatesTable extends SyncStates
    with TableInfo<$SyncStatesTable, SyncState> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $SyncStatesTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _subjectMeta = const VerificationMeta(
    'subject',
  );
  @override
  late final GeneratedColumn<String> subject = GeneratedColumn<String>(
    'subject',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _tenantIdMeta = const VerificationMeta(
    'tenantId',
  );
  @override
  late final GeneratedColumn<String> tenantId = GeneratedColumn<String>(
    'tenant_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _cursorMeta = const VerificationMeta('cursor');
  @override
  late final GeneratedColumn<String> cursor = GeneratedColumn<String>(
    'cursor',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _updatedAtMeta = const VerificationMeta(
    'updatedAt',
  );
  @override
  late final GeneratedColumn<DateTime> updatedAt = GeneratedColumn<DateTime>(
    'updated_at',
    aliasedName,
    false,
    type: DriftSqlType.dateTime,
    requiredDuringInsert: true,
  );
  @override
  List<GeneratedColumn> get $columns => [subject, tenantId, cursor, updatedAt];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'sync_states';
  @override
  VerificationContext validateIntegrity(
    Insertable<SyncState> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('subject')) {
      context.handle(
        _subjectMeta,
        subject.isAcceptableOrUnknown(data['subject']!, _subjectMeta),
      );
    } else if (isInserting) {
      context.missing(_subjectMeta);
    }
    if (data.containsKey('tenant_id')) {
      context.handle(
        _tenantIdMeta,
        tenantId.isAcceptableOrUnknown(data['tenant_id']!, _tenantIdMeta),
      );
    } else if (isInserting) {
      context.missing(_tenantIdMeta);
    }
    if (data.containsKey('cursor')) {
      context.handle(
        _cursorMeta,
        cursor.isAcceptableOrUnknown(data['cursor']!, _cursorMeta),
      );
    }
    if (data.containsKey('updated_at')) {
      context.handle(
        _updatedAtMeta,
        updatedAt.isAcceptableOrUnknown(data['updated_at']!, _updatedAtMeta),
      );
    } else if (isInserting) {
      context.missing(_updatedAtMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {subject, tenantId};
  @override
  SyncState map(Map<String, dynamic> data, {String? tablePrefix}) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return SyncState(
      subject: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}subject'],
      )!,
      tenantId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}tenant_id'],
      )!,
      cursor: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}cursor'],
      ),
      updatedAt: attachedDatabase.typeMapping.read(
        DriftSqlType.dateTime,
        data['${effectivePrefix}updated_at'],
      )!,
    );
  }

  @override
  $SyncStatesTable createAlias(String alias) {
    return $SyncStatesTable(attachedDatabase, alias);
  }
}

class SyncState extends DataClass implements Insertable<SyncState> {
  final String subject;
  final String tenantId;
  final String? cursor;
  final DateTime updatedAt;
  const SyncState({
    required this.subject,
    required this.tenantId,
    this.cursor,
    required this.updatedAt,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['subject'] = Variable<String>(subject);
    map['tenant_id'] = Variable<String>(tenantId);
    if (!nullToAbsent || cursor != null) {
      map['cursor'] = Variable<String>(cursor);
    }
    map['updated_at'] = Variable<DateTime>(updatedAt);
    return map;
  }

  SyncStatesCompanion toCompanion(bool nullToAbsent) {
    return SyncStatesCompanion(
      subject: Value(subject),
      tenantId: Value(tenantId),
      cursor: cursor == null && nullToAbsent
          ? const Value.absent()
          : Value(cursor),
      updatedAt: Value(updatedAt),
    );
  }

  factory SyncState.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return SyncState(
      subject: serializer.fromJson<String>(json['subject']),
      tenantId: serializer.fromJson<String>(json['tenantId']),
      cursor: serializer.fromJson<String?>(json['cursor']),
      updatedAt: serializer.fromJson<DateTime>(json['updatedAt']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'subject': serializer.toJson<String>(subject),
      'tenantId': serializer.toJson<String>(tenantId),
      'cursor': serializer.toJson<String?>(cursor),
      'updatedAt': serializer.toJson<DateTime>(updatedAt),
    };
  }

  SyncState copyWith({
    String? subject,
    String? tenantId,
    Value<String?> cursor = const Value.absent(),
    DateTime? updatedAt,
  }) => SyncState(
    subject: subject ?? this.subject,
    tenantId: tenantId ?? this.tenantId,
    cursor: cursor.present ? cursor.value : this.cursor,
    updatedAt: updatedAt ?? this.updatedAt,
  );
  SyncState copyWithCompanion(SyncStatesCompanion data) {
    return SyncState(
      subject: data.subject.present ? data.subject.value : this.subject,
      tenantId: data.tenantId.present ? data.tenantId.value : this.tenantId,
      cursor: data.cursor.present ? data.cursor.value : this.cursor,
      updatedAt: data.updatedAt.present ? data.updatedAt.value : this.updatedAt,
    );
  }

  @override
  String toString() {
    return (StringBuffer('SyncState(')
          ..write('subject: $subject, ')
          ..write('tenantId: $tenantId, ')
          ..write('cursor: $cursor, ')
          ..write('updatedAt: $updatedAt')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(subject, tenantId, cursor, updatedAt);
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is SyncState &&
          other.subject == this.subject &&
          other.tenantId == this.tenantId &&
          other.cursor == this.cursor &&
          other.updatedAt == this.updatedAt);
}

class SyncStatesCompanion extends UpdateCompanion<SyncState> {
  final Value<String> subject;
  final Value<String> tenantId;
  final Value<String?> cursor;
  final Value<DateTime> updatedAt;
  final Value<int> rowid;
  const SyncStatesCompanion({
    this.subject = const Value.absent(),
    this.tenantId = const Value.absent(),
    this.cursor = const Value.absent(),
    this.updatedAt = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  SyncStatesCompanion.insert({
    required String subject,
    required String tenantId,
    this.cursor = const Value.absent(),
    required DateTime updatedAt,
    this.rowid = const Value.absent(),
  }) : subject = Value(subject),
       tenantId = Value(tenantId),
       updatedAt = Value(updatedAt);
  static Insertable<SyncState> custom({
    Expression<String>? subject,
    Expression<String>? tenantId,
    Expression<String>? cursor,
    Expression<DateTime>? updatedAt,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (subject != null) 'subject': subject,
      if (tenantId != null) 'tenant_id': tenantId,
      if (cursor != null) 'cursor': cursor,
      if (updatedAt != null) 'updated_at': updatedAt,
      if (rowid != null) 'rowid': rowid,
    });
  }

  SyncStatesCompanion copyWith({
    Value<String>? subject,
    Value<String>? tenantId,
    Value<String?>? cursor,
    Value<DateTime>? updatedAt,
    Value<int>? rowid,
  }) {
    return SyncStatesCompanion(
      subject: subject ?? this.subject,
      tenantId: tenantId ?? this.tenantId,
      cursor: cursor ?? this.cursor,
      updatedAt: updatedAt ?? this.updatedAt,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (subject.present) {
      map['subject'] = Variable<String>(subject.value);
    }
    if (tenantId.present) {
      map['tenant_id'] = Variable<String>(tenantId.value);
    }
    if (cursor.present) {
      map['cursor'] = Variable<String>(cursor.value);
    }
    if (updatedAt.present) {
      map['updated_at'] = Variable<DateTime>(updatedAt.value);
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('SyncStatesCompanion(')
          ..write('subject: $subject, ')
          ..write('tenantId: $tenantId, ')
          ..write('cursor: $cursor, ')
          ..write('updatedAt: $updatedAt, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

abstract class _$OfflineDatabase extends GeneratedDatabase {
  _$OfflineDatabase(QueryExecutor e) : super(e);
  $OfflineDatabaseManager get managers => $OfflineDatabaseManager(this);
  late final $CachedResourcesTable cachedResources = $CachedResourcesTable(
    this,
  );
  late final $PendingSyncOperationsTable pendingSyncOperations =
      $PendingSyncOperationsTable(this);
  late final $SyncInboxTable syncInbox = $SyncInboxTable(this);
  late final $SyncStatesTable syncStates = $SyncStatesTable(this);
  @override
  Iterable<TableInfo<Table, Object?>> get allTables =>
      allSchemaEntities.whereType<TableInfo<Table, Object?>>();
  @override
  List<DatabaseSchemaEntity> get allSchemaEntities => [
    cachedResources,
    pendingSyncOperations,
    syncInbox,
    syncStates,
  ];
}

typedef $$CachedResourcesTableCreateCompanionBuilder =
    CachedResourcesCompanion Function({
      required String subject,
      required String tenantId,
      required String entityType,
      required String entityId,
      required String payloadJson,
      Value<int> version,
      Value<bool> isDeleted,
      required DateTime updatedAt,
      Value<int> rowid,
    });
typedef $$CachedResourcesTableUpdateCompanionBuilder =
    CachedResourcesCompanion Function({
      Value<String> subject,
      Value<String> tenantId,
      Value<String> entityType,
      Value<String> entityId,
      Value<String> payloadJson,
      Value<int> version,
      Value<bool> isDeleted,
      Value<DateTime> updatedAt,
      Value<int> rowid,
    });

class $$CachedResourcesTableFilterComposer
    extends Composer<_$OfflineDatabase, $CachedResourcesTable> {
  $$CachedResourcesTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get subject => $composableBuilder(
    column: $table.subject,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get tenantId => $composableBuilder(
    column: $table.tenantId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get entityType => $composableBuilder(
    column: $table.entityType,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get entityId => $composableBuilder(
    column: $table.entityId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get payloadJson => $composableBuilder(
    column: $table.payloadJson,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get version => $composableBuilder(
    column: $table.version,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<bool> get isDeleted => $composableBuilder(
    column: $table.isDeleted,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<DateTime> get updatedAt => $composableBuilder(
    column: $table.updatedAt,
    builder: (column) => ColumnFilters(column),
  );
}

class $$CachedResourcesTableOrderingComposer
    extends Composer<_$OfflineDatabase, $CachedResourcesTable> {
  $$CachedResourcesTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get subject => $composableBuilder(
    column: $table.subject,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get tenantId => $composableBuilder(
    column: $table.tenantId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get entityType => $composableBuilder(
    column: $table.entityType,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get entityId => $composableBuilder(
    column: $table.entityId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get payloadJson => $composableBuilder(
    column: $table.payloadJson,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get version => $composableBuilder(
    column: $table.version,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<bool> get isDeleted => $composableBuilder(
    column: $table.isDeleted,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<DateTime> get updatedAt => $composableBuilder(
    column: $table.updatedAt,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$CachedResourcesTableAnnotationComposer
    extends Composer<_$OfflineDatabase, $CachedResourcesTable> {
  $$CachedResourcesTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get subject =>
      $composableBuilder(column: $table.subject, builder: (column) => column);

  GeneratedColumn<String> get tenantId =>
      $composableBuilder(column: $table.tenantId, builder: (column) => column);

  GeneratedColumn<String> get entityType => $composableBuilder(
    column: $table.entityType,
    builder: (column) => column,
  );

  GeneratedColumn<String> get entityId =>
      $composableBuilder(column: $table.entityId, builder: (column) => column);

  GeneratedColumn<String> get payloadJson => $composableBuilder(
    column: $table.payloadJson,
    builder: (column) => column,
  );

  GeneratedColumn<int> get version =>
      $composableBuilder(column: $table.version, builder: (column) => column);

  GeneratedColumn<bool> get isDeleted =>
      $composableBuilder(column: $table.isDeleted, builder: (column) => column);

  GeneratedColumn<DateTime> get updatedAt =>
      $composableBuilder(column: $table.updatedAt, builder: (column) => column);
}

class $$CachedResourcesTableTableManager
    extends
        RootTableManager<
          _$OfflineDatabase,
          $CachedResourcesTable,
          CachedResource,
          $$CachedResourcesTableFilterComposer,
          $$CachedResourcesTableOrderingComposer,
          $$CachedResourcesTableAnnotationComposer,
          $$CachedResourcesTableCreateCompanionBuilder,
          $$CachedResourcesTableUpdateCompanionBuilder,
          (
            CachedResource,
            BaseReferences<
              _$OfflineDatabase,
              $CachedResourcesTable,
              CachedResource
            >,
          ),
          CachedResource,
          PrefetchHooks Function()
        > {
  $$CachedResourcesTableTableManager(
    _$OfflineDatabase db,
    $CachedResourcesTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$CachedResourcesTableFilterComposer($db: db, $table: table),
          createOrderingComposer: () =>
              $$CachedResourcesTableOrderingComposer($db: db, $table: table),
          createComputedFieldComposer: () =>
              $$CachedResourcesTableAnnotationComposer($db: db, $table: table),
          updateCompanionCallback:
              ({
                Value<String> subject = const Value.absent(),
                Value<String> tenantId = const Value.absent(),
                Value<String> entityType = const Value.absent(),
                Value<String> entityId = const Value.absent(),
                Value<String> payloadJson = const Value.absent(),
                Value<int> version = const Value.absent(),
                Value<bool> isDeleted = const Value.absent(),
                Value<DateTime> updatedAt = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => CachedResourcesCompanion(
                subject: subject,
                tenantId: tenantId,
                entityType: entityType,
                entityId: entityId,
                payloadJson: payloadJson,
                version: version,
                isDeleted: isDeleted,
                updatedAt: updatedAt,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String subject,
                required String tenantId,
                required String entityType,
                required String entityId,
                required String payloadJson,
                Value<int> version = const Value.absent(),
                Value<bool> isDeleted = const Value.absent(),
                required DateTime updatedAt,
                Value<int> rowid = const Value.absent(),
              }) => CachedResourcesCompanion.insert(
                subject: subject,
                tenantId: tenantId,
                entityType: entityType,
                entityId: entityId,
                payloadJson: payloadJson,
                version: version,
                isDeleted: isDeleted,
                updatedAt: updatedAt,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map(
                (e) => (
                  e.readTable<$CachedResourcesTable, CachedResource>(table),
                  BaseReferences<
                    _$OfflineDatabase,
                    $CachedResourcesTable,
                    CachedResource
                  >(db, table, e),
                ),
              )
              .toList(),
          prefetchHooksCallback: null,
        ),
      );
}

typedef $$CachedResourcesTableProcessedTableManager =
    ProcessedTableManager<
      _$OfflineDatabase,
      $CachedResourcesTable,
      CachedResource,
      $$CachedResourcesTableFilterComposer,
      $$CachedResourcesTableOrderingComposer,
      $$CachedResourcesTableAnnotationComposer,
      $$CachedResourcesTableCreateCompanionBuilder,
      $$CachedResourcesTableUpdateCompanionBuilder,
      (
        CachedResource,
        BaseReferences<
          _$OfflineDatabase,
          $CachedResourcesTable,
          CachedResource
        >,
      ),
      CachedResource,
      PrefetchHooks Function()
    >;
typedef $$PendingSyncOperationsTableCreateCompanionBuilder =
    PendingSyncOperationsCompanion Function({
      required String operationId,
      required String subject,
      required String tenantId,
      required String deviceId,
      required String entityId,
      required String entityType,
      required String operationType,
      required String endpoint,
      required String payloadJson,
      Value<int> version,
      Value<String> status,
      Value<int> retryCount,
      Value<String?> lastError,
      required DateTime clientTimestamp,
      Value<DateTime?> nextAttemptAt,
      Value<int> rowid,
    });
typedef $$PendingSyncOperationsTableUpdateCompanionBuilder =
    PendingSyncOperationsCompanion Function({
      Value<String> operationId,
      Value<String> subject,
      Value<String> tenantId,
      Value<String> deviceId,
      Value<String> entityId,
      Value<String> entityType,
      Value<String> operationType,
      Value<String> endpoint,
      Value<String> payloadJson,
      Value<int> version,
      Value<String> status,
      Value<int> retryCount,
      Value<String?> lastError,
      Value<DateTime> clientTimestamp,
      Value<DateTime?> nextAttemptAt,
      Value<int> rowid,
    });

class $$PendingSyncOperationsTableFilterComposer
    extends Composer<_$OfflineDatabase, $PendingSyncOperationsTable> {
  $$PendingSyncOperationsTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get operationId => $composableBuilder(
    column: $table.operationId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get subject => $composableBuilder(
    column: $table.subject,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get tenantId => $composableBuilder(
    column: $table.tenantId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get deviceId => $composableBuilder(
    column: $table.deviceId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get entityId => $composableBuilder(
    column: $table.entityId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get entityType => $composableBuilder(
    column: $table.entityType,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get operationType => $composableBuilder(
    column: $table.operationType,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get endpoint => $composableBuilder(
    column: $table.endpoint,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get payloadJson => $composableBuilder(
    column: $table.payloadJson,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get version => $composableBuilder(
    column: $table.version,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get status => $composableBuilder(
    column: $table.status,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get retryCount => $composableBuilder(
    column: $table.retryCount,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastError => $composableBuilder(
    column: $table.lastError,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<DateTime> get clientTimestamp => $composableBuilder(
    column: $table.clientTimestamp,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<DateTime> get nextAttemptAt => $composableBuilder(
    column: $table.nextAttemptAt,
    builder: (column) => ColumnFilters(column),
  );
}

class $$PendingSyncOperationsTableOrderingComposer
    extends Composer<_$OfflineDatabase, $PendingSyncOperationsTable> {
  $$PendingSyncOperationsTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get operationId => $composableBuilder(
    column: $table.operationId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get subject => $composableBuilder(
    column: $table.subject,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get tenantId => $composableBuilder(
    column: $table.tenantId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get deviceId => $composableBuilder(
    column: $table.deviceId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get entityId => $composableBuilder(
    column: $table.entityId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get entityType => $composableBuilder(
    column: $table.entityType,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get operationType => $composableBuilder(
    column: $table.operationType,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get endpoint => $composableBuilder(
    column: $table.endpoint,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get payloadJson => $composableBuilder(
    column: $table.payloadJson,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get version => $composableBuilder(
    column: $table.version,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get status => $composableBuilder(
    column: $table.status,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get retryCount => $composableBuilder(
    column: $table.retryCount,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastError => $composableBuilder(
    column: $table.lastError,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<DateTime> get clientTimestamp => $composableBuilder(
    column: $table.clientTimestamp,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<DateTime> get nextAttemptAt => $composableBuilder(
    column: $table.nextAttemptAt,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$PendingSyncOperationsTableAnnotationComposer
    extends Composer<_$OfflineDatabase, $PendingSyncOperationsTable> {
  $$PendingSyncOperationsTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get operationId => $composableBuilder(
    column: $table.operationId,
    builder: (column) => column,
  );

  GeneratedColumn<String> get subject =>
      $composableBuilder(column: $table.subject, builder: (column) => column);

  GeneratedColumn<String> get tenantId =>
      $composableBuilder(column: $table.tenantId, builder: (column) => column);

  GeneratedColumn<String> get deviceId =>
      $composableBuilder(column: $table.deviceId, builder: (column) => column);

  GeneratedColumn<String> get entityId =>
      $composableBuilder(column: $table.entityId, builder: (column) => column);

  GeneratedColumn<String> get entityType => $composableBuilder(
    column: $table.entityType,
    builder: (column) => column,
  );

  GeneratedColumn<String> get operationType => $composableBuilder(
    column: $table.operationType,
    builder: (column) => column,
  );

  GeneratedColumn<String> get endpoint =>
      $composableBuilder(column: $table.endpoint, builder: (column) => column);

  GeneratedColumn<String> get payloadJson => $composableBuilder(
    column: $table.payloadJson,
    builder: (column) => column,
  );

  GeneratedColumn<int> get version =>
      $composableBuilder(column: $table.version, builder: (column) => column);

  GeneratedColumn<String> get status =>
      $composableBuilder(column: $table.status, builder: (column) => column);

  GeneratedColumn<int> get retryCount => $composableBuilder(
    column: $table.retryCount,
    builder: (column) => column,
  );

  GeneratedColumn<String> get lastError =>
      $composableBuilder(column: $table.lastError, builder: (column) => column);

  GeneratedColumn<DateTime> get clientTimestamp => $composableBuilder(
    column: $table.clientTimestamp,
    builder: (column) => column,
  );

  GeneratedColumn<DateTime> get nextAttemptAt => $composableBuilder(
    column: $table.nextAttemptAt,
    builder: (column) => column,
  );
}

class $$PendingSyncOperationsTableTableManager
    extends
        RootTableManager<
          _$OfflineDatabase,
          $PendingSyncOperationsTable,
          PendingSyncOperation,
          $$PendingSyncOperationsTableFilterComposer,
          $$PendingSyncOperationsTableOrderingComposer,
          $$PendingSyncOperationsTableAnnotationComposer,
          $$PendingSyncOperationsTableCreateCompanionBuilder,
          $$PendingSyncOperationsTableUpdateCompanionBuilder,
          (
            PendingSyncOperation,
            BaseReferences<
              _$OfflineDatabase,
              $PendingSyncOperationsTable,
              PendingSyncOperation
            >,
          ),
          PendingSyncOperation,
          PrefetchHooks Function()
        > {
  $$PendingSyncOperationsTableTableManager(
    _$OfflineDatabase db,
    $PendingSyncOperationsTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$PendingSyncOperationsTableFilterComposer(
                $db: db,
                $table: table,
              ),
          createOrderingComposer: () =>
              $$PendingSyncOperationsTableOrderingComposer(
                $db: db,
                $table: table,
              ),
          createComputedFieldComposer: () =>
              $$PendingSyncOperationsTableAnnotationComposer(
                $db: db,
                $table: table,
              ),
          updateCompanionCallback:
              ({
                Value<String> operationId = const Value.absent(),
                Value<String> subject = const Value.absent(),
                Value<String> tenantId = const Value.absent(),
                Value<String> deviceId = const Value.absent(),
                Value<String> entityId = const Value.absent(),
                Value<String> entityType = const Value.absent(),
                Value<String> operationType = const Value.absent(),
                Value<String> endpoint = const Value.absent(),
                Value<String> payloadJson = const Value.absent(),
                Value<int> version = const Value.absent(),
                Value<String> status = const Value.absent(),
                Value<int> retryCount = const Value.absent(),
                Value<String?> lastError = const Value.absent(),
                Value<DateTime> clientTimestamp = const Value.absent(),
                Value<DateTime?> nextAttemptAt = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => PendingSyncOperationsCompanion(
                operationId: operationId,
                subject: subject,
                tenantId: tenantId,
                deviceId: deviceId,
                entityId: entityId,
                entityType: entityType,
                operationType: operationType,
                endpoint: endpoint,
                payloadJson: payloadJson,
                version: version,
                status: status,
                retryCount: retryCount,
                lastError: lastError,
                clientTimestamp: clientTimestamp,
                nextAttemptAt: nextAttemptAt,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String operationId,
                required String subject,
                required String tenantId,
                required String deviceId,
                required String entityId,
                required String entityType,
                required String operationType,
                required String endpoint,
                required String payloadJson,
                Value<int> version = const Value.absent(),
                Value<String> status = const Value.absent(),
                Value<int> retryCount = const Value.absent(),
                Value<String?> lastError = const Value.absent(),
                required DateTime clientTimestamp,
                Value<DateTime?> nextAttemptAt = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => PendingSyncOperationsCompanion.insert(
                operationId: operationId,
                subject: subject,
                tenantId: tenantId,
                deviceId: deviceId,
                entityId: entityId,
                entityType: entityType,
                operationType: operationType,
                endpoint: endpoint,
                payloadJson: payloadJson,
                version: version,
                status: status,
                retryCount: retryCount,
                lastError: lastError,
                clientTimestamp: clientTimestamp,
                nextAttemptAt: nextAttemptAt,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map(
                (e) => (
                  e.readTable<
                    $PendingSyncOperationsTable,
                    PendingSyncOperation
                  >(table),
                  BaseReferences<
                    _$OfflineDatabase,
                    $PendingSyncOperationsTable,
                    PendingSyncOperation
                  >(db, table, e),
                ),
              )
              .toList(),
          prefetchHooksCallback: null,
        ),
      );
}

typedef $$PendingSyncOperationsTableProcessedTableManager =
    ProcessedTableManager<
      _$OfflineDatabase,
      $PendingSyncOperationsTable,
      PendingSyncOperation,
      $$PendingSyncOperationsTableFilterComposer,
      $$PendingSyncOperationsTableOrderingComposer,
      $$PendingSyncOperationsTableAnnotationComposer,
      $$PendingSyncOperationsTableCreateCompanionBuilder,
      $$PendingSyncOperationsTableUpdateCompanionBuilder,
      (
        PendingSyncOperation,
        BaseReferences<
          _$OfflineDatabase,
          $PendingSyncOperationsTable,
          PendingSyncOperation
        >,
      ),
      PendingSyncOperation,
      PrefetchHooks Function()
    >;
typedef $$SyncInboxTableCreateCompanionBuilder =
    SyncInboxCompanion Function({
      required String operationId,
      required String subject,
      required String tenantId,
      required DateTime receivedAt,
      Value<int> rowid,
    });
typedef $$SyncInboxTableUpdateCompanionBuilder =
    SyncInboxCompanion Function({
      Value<String> operationId,
      Value<String> subject,
      Value<String> tenantId,
      Value<DateTime> receivedAt,
      Value<int> rowid,
    });

class $$SyncInboxTableFilterComposer
    extends Composer<_$OfflineDatabase, $SyncInboxTable> {
  $$SyncInboxTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get operationId => $composableBuilder(
    column: $table.operationId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get subject => $composableBuilder(
    column: $table.subject,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get tenantId => $composableBuilder(
    column: $table.tenantId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<DateTime> get receivedAt => $composableBuilder(
    column: $table.receivedAt,
    builder: (column) => ColumnFilters(column),
  );
}

class $$SyncInboxTableOrderingComposer
    extends Composer<_$OfflineDatabase, $SyncInboxTable> {
  $$SyncInboxTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get operationId => $composableBuilder(
    column: $table.operationId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get subject => $composableBuilder(
    column: $table.subject,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get tenantId => $composableBuilder(
    column: $table.tenantId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<DateTime> get receivedAt => $composableBuilder(
    column: $table.receivedAt,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$SyncInboxTableAnnotationComposer
    extends Composer<_$OfflineDatabase, $SyncInboxTable> {
  $$SyncInboxTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get operationId => $composableBuilder(
    column: $table.operationId,
    builder: (column) => column,
  );

  GeneratedColumn<String> get subject =>
      $composableBuilder(column: $table.subject, builder: (column) => column);

  GeneratedColumn<String> get tenantId =>
      $composableBuilder(column: $table.tenantId, builder: (column) => column);

  GeneratedColumn<DateTime> get receivedAt => $composableBuilder(
    column: $table.receivedAt,
    builder: (column) => column,
  );
}

class $$SyncInboxTableTableManager
    extends
        RootTableManager<
          _$OfflineDatabase,
          $SyncInboxTable,
          SyncInboxData,
          $$SyncInboxTableFilterComposer,
          $$SyncInboxTableOrderingComposer,
          $$SyncInboxTableAnnotationComposer,
          $$SyncInboxTableCreateCompanionBuilder,
          $$SyncInboxTableUpdateCompanionBuilder,
          (
            SyncInboxData,
            BaseReferences<_$OfflineDatabase, $SyncInboxTable, SyncInboxData>,
          ),
          SyncInboxData,
          PrefetchHooks Function()
        > {
  $$SyncInboxTableTableManager(_$OfflineDatabase db, $SyncInboxTable table)
    : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$SyncInboxTableFilterComposer($db: db, $table: table),
          createOrderingComposer: () =>
              $$SyncInboxTableOrderingComposer($db: db, $table: table),
          createComputedFieldComposer: () =>
              $$SyncInboxTableAnnotationComposer($db: db, $table: table),
          updateCompanionCallback:
              ({
                Value<String> operationId = const Value.absent(),
                Value<String> subject = const Value.absent(),
                Value<String> tenantId = const Value.absent(),
                Value<DateTime> receivedAt = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => SyncInboxCompanion(
                operationId: operationId,
                subject: subject,
                tenantId: tenantId,
                receivedAt: receivedAt,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String operationId,
                required String subject,
                required String tenantId,
                required DateTime receivedAt,
                Value<int> rowid = const Value.absent(),
              }) => SyncInboxCompanion.insert(
                operationId: operationId,
                subject: subject,
                tenantId: tenantId,
                receivedAt: receivedAt,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map(
                (e) => (
                  e.readTable<$SyncInboxTable, SyncInboxData>(table),
                  BaseReferences<
                    _$OfflineDatabase,
                    $SyncInboxTable,
                    SyncInboxData
                  >(db, table, e),
                ),
              )
              .toList(),
          prefetchHooksCallback: null,
        ),
      );
}

typedef $$SyncInboxTableProcessedTableManager =
    ProcessedTableManager<
      _$OfflineDatabase,
      $SyncInboxTable,
      SyncInboxData,
      $$SyncInboxTableFilterComposer,
      $$SyncInboxTableOrderingComposer,
      $$SyncInboxTableAnnotationComposer,
      $$SyncInboxTableCreateCompanionBuilder,
      $$SyncInboxTableUpdateCompanionBuilder,
      (
        SyncInboxData,
        BaseReferences<_$OfflineDatabase, $SyncInboxTable, SyncInboxData>,
      ),
      SyncInboxData,
      PrefetchHooks Function()
    >;
typedef $$SyncStatesTableCreateCompanionBuilder =
    SyncStatesCompanion Function({
      required String subject,
      required String tenantId,
      Value<String?> cursor,
      required DateTime updatedAt,
      Value<int> rowid,
    });
typedef $$SyncStatesTableUpdateCompanionBuilder =
    SyncStatesCompanion Function({
      Value<String> subject,
      Value<String> tenantId,
      Value<String?> cursor,
      Value<DateTime> updatedAt,
      Value<int> rowid,
    });

class $$SyncStatesTableFilterComposer
    extends Composer<_$OfflineDatabase, $SyncStatesTable> {
  $$SyncStatesTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get subject => $composableBuilder(
    column: $table.subject,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get tenantId => $composableBuilder(
    column: $table.tenantId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get cursor => $composableBuilder(
    column: $table.cursor,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<DateTime> get updatedAt => $composableBuilder(
    column: $table.updatedAt,
    builder: (column) => ColumnFilters(column),
  );
}

class $$SyncStatesTableOrderingComposer
    extends Composer<_$OfflineDatabase, $SyncStatesTable> {
  $$SyncStatesTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get subject => $composableBuilder(
    column: $table.subject,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get tenantId => $composableBuilder(
    column: $table.tenantId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get cursor => $composableBuilder(
    column: $table.cursor,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<DateTime> get updatedAt => $composableBuilder(
    column: $table.updatedAt,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$SyncStatesTableAnnotationComposer
    extends Composer<_$OfflineDatabase, $SyncStatesTable> {
  $$SyncStatesTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get subject =>
      $composableBuilder(column: $table.subject, builder: (column) => column);

  GeneratedColumn<String> get tenantId =>
      $composableBuilder(column: $table.tenantId, builder: (column) => column);

  GeneratedColumn<String> get cursor =>
      $composableBuilder(column: $table.cursor, builder: (column) => column);

  GeneratedColumn<DateTime> get updatedAt =>
      $composableBuilder(column: $table.updatedAt, builder: (column) => column);
}

class $$SyncStatesTableTableManager
    extends
        RootTableManager<
          _$OfflineDatabase,
          $SyncStatesTable,
          SyncState,
          $$SyncStatesTableFilterComposer,
          $$SyncStatesTableOrderingComposer,
          $$SyncStatesTableAnnotationComposer,
          $$SyncStatesTableCreateCompanionBuilder,
          $$SyncStatesTableUpdateCompanionBuilder,
          (
            SyncState,
            BaseReferences<_$OfflineDatabase, $SyncStatesTable, SyncState>,
          ),
          SyncState,
          PrefetchHooks Function()
        > {
  $$SyncStatesTableTableManager(_$OfflineDatabase db, $SyncStatesTable table)
    : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$SyncStatesTableFilterComposer($db: db, $table: table),
          createOrderingComposer: () =>
              $$SyncStatesTableOrderingComposer($db: db, $table: table),
          createComputedFieldComposer: () =>
              $$SyncStatesTableAnnotationComposer($db: db, $table: table),
          updateCompanionCallback:
              ({
                Value<String> subject = const Value.absent(),
                Value<String> tenantId = const Value.absent(),
                Value<String?> cursor = const Value.absent(),
                Value<DateTime> updatedAt = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => SyncStatesCompanion(
                subject: subject,
                tenantId: tenantId,
                cursor: cursor,
                updatedAt: updatedAt,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String subject,
                required String tenantId,
                Value<String?> cursor = const Value.absent(),
                required DateTime updatedAt,
                Value<int> rowid = const Value.absent(),
              }) => SyncStatesCompanion.insert(
                subject: subject,
                tenantId: tenantId,
                cursor: cursor,
                updatedAt: updatedAt,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map(
                (e) => (
                  e.readTable<$SyncStatesTable, SyncState>(table),
                  BaseReferences<
                    _$OfflineDatabase,
                    $SyncStatesTable,
                    SyncState
                  >(db, table, e),
                ),
              )
              .toList(),
          prefetchHooksCallback: null,
        ),
      );
}

typedef $$SyncStatesTableProcessedTableManager =
    ProcessedTableManager<
      _$OfflineDatabase,
      $SyncStatesTable,
      SyncState,
      $$SyncStatesTableFilterComposer,
      $$SyncStatesTableOrderingComposer,
      $$SyncStatesTableAnnotationComposer,
      $$SyncStatesTableCreateCompanionBuilder,
      $$SyncStatesTableUpdateCompanionBuilder,
      (
        SyncState,
        BaseReferences<_$OfflineDatabase, $SyncStatesTable, SyncState>,
      ),
      SyncState,
      PrefetchHooks Function()
    >;

class $OfflineDatabaseManager {
  final _$OfflineDatabase _db;
  $OfflineDatabaseManager(this._db);
  $$CachedResourcesTableTableManager get cachedResources =>
      $$CachedResourcesTableTableManager(_db, _db.cachedResources);
  $$PendingSyncOperationsTableTableManager get pendingSyncOperations =>
      $$PendingSyncOperationsTableTableManager(_db, _db.pendingSyncOperations);
  $$SyncInboxTableTableManager get syncInbox =>
      $$SyncInboxTableTableManager(_db, _db.syncInbox);
  $$SyncStatesTableTableManager get syncStates =>
      $$SyncStatesTableTableManager(_db, _db.syncStates);
}
