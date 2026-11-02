-- CHECKPOINT 13 / RBAC TEST DATABASE ONLY — READ ONLY
DO $$
BEGIN
    IF current_database() <> 'rass_sprint1_tests' OR current_user <> 'neondb_owner' THEN
        RAISE EXCEPTION 'Unexpected database or validation user';
    END IF;

    IF (SELECT count(*) FROM app."__EFMigrationsHistory") <> 20
       OR NOT EXISTS (
           SELECT 1 FROM app."__EFMigrationsHistory"
           WHERE "MigrationId" = '20261102040000_RuntimeIdentityOperationalAclHardening')
       OR (SELECT max("MigrationId") FROM app."__EFMigrationsHistory") <> '20261102040000_RuntimeIdentityOperationalAclHardening'
       OR NOT has_schema_privilege('rass_app_runtime_v2', 'app', 'USAGE')
       OR has_schema_privilege('rass_app_runtime_v2', 'app', 'CREATE')
       OR EXISTS (
           SELECT 1
           FROM pg_namespace schema
           CROSS JOIN LATERAL aclexplode(COALESCE(schema.nspacl, acldefault('n', schema.nspowner))) privilege
           WHERE schema.nspname = 'app'
             AND privilege.grantee = 0
             AND privilege.privilege_type IN ('USAGE', 'CREATE')
       )
       OR has_schema_privilege('rass_platform_executor_owner_v1', 'app', 'CREATE')
       OR pg_has_role('neondb_owner', 'rass_platform_executor_owner_v1', 'SET')
       OR pg_has_role('neondb_owner', 'rass_platform_executor_owner_v1', 'USAGE')
       OR EXISTS (
           SELECT 1
           FROM pg_auth_members membership
           JOIN pg_roles grantor ON grantor.oid = membership.grantor
           JOIN pg_roles member_role ON member_role.oid = membership.member
           JOIN pg_roles granted_role ON granted_role.oid = membership.roleid
           WHERE grantor.rolname = 'neondb_owner'
             AND member_role.rolname = 'neondb_owner'
             AND granted_role.rolname = 'rass_platform_executor_owner_v1')
       OR EXISTS (
           SELECT 1
           FROM pg_proc p
           JOIN pg_namespace n ON n.oid = p.pronamespace
           CROSS JOIN LATERAL aclexplode(COALESCE(p.proacl, acldefault('f', p.proowner))) privilege
           WHERE n.nspname = 'app'
             AND p.oid IN ('app.platform_dashboard()'::regprocedure,
                           'app.platform_roles(text)'::regprocedure,
                           'app.platform_grant_superadmin(uuid,uuid,text,text,text,text)'::regprocedure,
                           'app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)'::regprocedure)
             AND privilege.grantee = 0
             AND privilege.privilege_type = 'EXECUTE')
       OR has_function_privilege('rass_app_runtime_v2', 'app.platform_dashboard()', 'EXECUTE')
       OR has_function_privilege('rass_app_runtime_v2', 'app.platform_roles(text)', 'EXECUTE')
       OR has_function_privilege('rass_app_runtime_v2', 'app.platform_grant_superadmin(uuid,uuid,text,text,text,text)', 'EXECUTE')
       OR has_function_privilege('rass_app_runtime_v2', 'app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)', 'EXECUTE')
       OR NOT has_table_privilege('rass_app_runtime_v2', 'app."TenantMemberships"', 'SELECT')
       OR NOT has_table_privilege('rass_app_runtime_v2', 'app."TenantMemberships"', 'INSERT')
       OR has_table_privilege('rass_app_runtime_v2', 'app."TenantMemberships"', 'UPDATE')
       OR has_table_privilege('rass_app_runtime_v2', 'app."TenantMemberships"', 'DELETE')
       OR NOT has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'SELECT')
       OR NOT has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'INSERT')
       OR NOT has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'UPDATE')
       OR has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'DELETE')
       OR NOT has_table_privilege('rass_app_runtime_v2', 'app."AuditEntries"', 'SELECT')
       OR NOT has_table_privilege('rass_app_runtime_v2', 'app."AuditEntries"', 'INSERT')
       OR has_table_privilege('rass_app_runtime_v2', 'app."AuditEntries"', 'UPDATE')
       OR has_table_privilege('rass_app_runtime_v2', 'app."AuditEntries"', 'DELETE')
       OR NOT has_table_privilege('rass_app_runtime_v2', 'app."Tenants"', 'SELECT')
       OR NOT has_table_privilege('rass_app_runtime_v2', 'app."Tenants"', 'INSERT')
       OR has_table_privilege('rass_app_runtime_v2', 'app."Tenants"', 'UPDATE')
       OR has_table_privilege('rass_app_runtime_v2', 'app."Tenants"', 'DELETE')
       OR NOT has_table_privilege('rass_app_runtime_v2', 'app."UserProfiles"', 'SELECT')
       OR NOT has_table_privilege('rass_app_runtime_v2', 'app."UserProfiles"', 'INSERT')
       OR NOT has_table_privilege('rass_app_runtime_v2', 'app."UserProfiles"', 'UPDATE')
       OR has_table_privilege('rass_app_runtime_v2', 'app."UserProfiles"', 'DELETE')
       OR has_table_privilege('rass_app_runtime_v2', 'app."__EFMigrationsHistory"', 'SELECT')
       OR NOT has_function_privilege('rass_platform_admin_v1', 'app.platform_dashboard()', 'EXECUTE')
       OR NOT has_function_privilege('rass_platform_admin_v1', 'app.platform_roles(text)', 'EXECUTE')
       OR NOT has_function_privilege('rass_platform_admin_v1', 'app.platform_grant_superadmin(uuid,uuid,text,text,text,text)', 'EXECUTE')
       OR NOT has_function_privilege('rass_platform_admin_v1', 'app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)', 'EXECUTE')
       OR EXISTS (
           SELECT 1 FROM pg_roles
           WHERE rolname IN ('rass_platform_admin_v1', 'rass_platform_executor_owner_v1')
             AND (rolsuper OR rolcreaterole OR rolcreatedb OR rolbypassrls
                  OR pg_has_role(oid, 'neon_superuser', 'MEMBER'))
       )
       OR (SELECT count(*)
           FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
           WHERE n.nspname = 'app'
             AND c.relname IN ('PlatformRoleAssignments', 'AuditEntries', 'Tenants', 'UserProfiles')
             AND c.relrowsecurity AND c.relforcerowsecurity) <> 4 THEN
        RAISE EXCEPTION 'RBAC test database post-validation failed: runtime operational ACL is invalid';
    END IF;
END $$;

SELECT "MigrationId", "ProductVersion"
FROM app."__EFMigrationsHistory"
ORDER BY "MigrationId";

SELECT p.proname, p.prosecdef AS security_definer,
       pg_get_userbyid(p.proowner) AS owner,
       has_function_privilege('rass_app_runtime_v2', p.oid, 'EXECUTE') AS runtime_can_execute,
       has_function_privilege('rass_platform_admin_v1', p.oid, 'EXECUTE') AS admin_can_execute,
       EXISTS (SELECT 1 FROM aclexplode(COALESCE(p.proacl, acldefault('f', p.proowner))) privilege
               WHERE privilege.grantee = 0 AND privilege.privilege_type = 'EXECUTE') AS public_can_execute
FROM pg_proc p
JOIN pg_namespace n ON n.oid = p.pronamespace
WHERE n.nspname = 'app'
  AND p.oid IN ('app.platform_dashboard()'::regprocedure,
                'app.platform_roles(text)'::regprocedure,
                'app.platform_grant_superadmin(uuid,uuid,text,text,text,text)'::regprocedure,
                'app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)'::regprocedure)
ORDER BY p.proname;

SELECT c.relname, c.relrowsecurity, c.relforcerowsecurity
FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'app'
  AND c.relname IN ('PlatformRoleAssignments', 'AuditEntries', 'Tenants', 'UserProfiles')
ORDER BY c.relname;

SELECT has_schema_privilege('rass_app_runtime_v2', 'app', 'USAGE') AS runtime_schema_usage,
       has_schema_privilege('rass_app_runtime_v2', 'app', 'CREATE') AS runtime_schema_create,
       has_table_privilege('rass_app_runtime_v2', 'app."Accounts"', 'SELECT') AS runtime_accounts_read,
       has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'SELECT') AS runtime_sessions_read,
       has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'INSERT') AS runtime_sessions_insert,
       has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'UPDATE') AS runtime_sessions_update,
       has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'DELETE') AS runtime_sessions_delete,
       has_table_privilege('rass_app_runtime_v2', 'app."AuditEntries"', 'INSERT') AS runtime_audit_insert,
       has_table_privilege('rass_app_runtime_v2', 'app."__EFMigrationsHistory"', 'SELECT') AS runtime_ef_history_read,
       has_function_privilege('rass_app_runtime_v2', 'app.assert_transfer_ledger()', 'EXECUTE') AS runtime_transfer_assert;
