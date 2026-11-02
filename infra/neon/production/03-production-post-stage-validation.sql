-- Checkpoint 14 post-stage validation. READ ONLY.
SELECT "MigrationId", "ProductVersion"
FROM app."__EFMigrationsHistory"
ORDER BY "MigrationId";

SELECT p.proname, p.prosecdef AS security_definer,
       pg_get_userbyid(p.proowner) AS owner,
       has_function_privilege('rass_app_runtime_v2', p.oid, 'EXECUTE') AS runtime_execute,
       has_function_privilege('rass_platform_admin_v1', p.oid, 'EXECUTE') AS admin_execute,
       EXISTS (SELECT 1 FROM aclexplode(COALESCE(p.proacl, acldefault('f', p.proowner))) acl
               WHERE acl.grantee = 0 AND acl.privilege_type = 'EXECUTE') AS public_execute
FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace
WHERE n.nspname = 'app' AND p.proname LIKE 'platform_%'
ORDER BY p.proname;

SELECT has_schema_privilege('rass_app_runtime_v2', 'app', 'USAGE') AS runtime_usage,
       has_schema_privilege('rass_app_runtime_v2', 'app', 'CREATE') AS runtime_create,
       has_table_privilege('rass_app_runtime_v2', 'app."Tenants"', 'DELETE') AS runtime_tenant_delete,
       has_table_privilege('rass_app_runtime_v2', 'app."UserProfiles"', 'DELETE') AS runtime_profile_delete,
       has_table_privilege('rass_app_runtime_v2', 'app."__EFMigrationsHistory"', 'SELECT') AS runtime_migration_history_read,
       has_schema_privilege('PUBLIC', 'app', 'USAGE') AS public_schema_usage;
