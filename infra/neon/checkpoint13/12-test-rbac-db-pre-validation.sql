-- CHECKPOINT 13 / RBAC TEST DATABASE ONLY — READ ONLY
-- This phase runs before temporary test seeding. Every failure names the
-- missing or excessive ACL without disclosing any connection material.
DO $$
BEGIN
    IF current_database() <> 'rass_sprint1_tests' OR current_user <> 'neondb_owner' THEN
        RAISE EXCEPTION 'Unexpected database or validation user';
    END IF;

    IF (SELECT count(*) FROM app."__EFMigrationsHistory") <> 20
       OR NOT EXISTS (
           SELECT 1 FROM app."__EFMigrationsHistory"
           WHERE "MigrationId" = '20261102040000_RuntimeIdentityOperationalAclHardening')
       OR (SELECT max("MigrationId") FROM app."__EFMigrationsHistory") <> '20261102040000_RuntimeIdentityOperationalAclHardening' THEN
        RAISE EXCEPTION 'Expected RuntimeIdentityOperationalAclHardening migration is not applied';
    END IF;

    IF NOT has_schema_privilege('rass_app_runtime_v2', 'app', 'USAGE') THEN
        RAISE EXCEPTION 'Runtime ACL missing: schema app USAGE';
    ELSIF has_schema_privilege('rass_app_runtime_v2', 'app', 'CREATE') THEN
        RAISE EXCEPTION 'Runtime ACL excessive: schema app CREATE';
    ELSIF NOT has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'SELECT') THEN
        RAISE EXCEPTION 'Runtime ACL missing: UserSessions SELECT';
    ELSIF NOT has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'INSERT') THEN
        RAISE EXCEPTION 'Runtime ACL missing: UserSessions INSERT';
    ELSIF NOT has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'UPDATE') THEN
        RAISE EXCEPTION 'Runtime ACL missing: UserSessions UPDATE';
    ELSIF has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'DELETE') THEN
        RAISE EXCEPTION 'Runtime ACL excessive: UserSessions DELETE';
    ELSIF NOT has_table_privilege('rass_app_runtime_v2', 'app."AuditEntries"', 'INSERT') THEN
        RAISE EXCEPTION 'Runtime ACL missing: AuditEntries INSERT';
    ELSIF NOT has_table_privilege('rass_app_runtime_v2', 'app."TenantMemberships"', 'SELECT') THEN
        RAISE EXCEPTION 'Runtime ACL missing: TenantMemberships SELECT';
    ELSIF NOT has_table_privilege('rass_app_runtime_v2', 'app."TenantMemberships"', 'INSERT') THEN
        RAISE EXCEPTION 'Runtime ACL missing: TenantMemberships INSERT';
    ELSIF NOT has_table_privilege('rass_app_runtime_v2', 'app."Tenants"', 'SELECT') THEN
        RAISE EXCEPTION 'Runtime ACL missing: Tenants SELECT';
    ELSIF NOT has_table_privilege('rass_app_runtime_v2', 'app."Tenants"', 'INSERT') THEN
        RAISE EXCEPTION 'Runtime ACL missing: Tenants INSERT';
    ELSIF NOT has_table_privilege('rass_app_runtime_v2', 'app."UserProfiles"', 'SELECT') THEN
        RAISE EXCEPTION 'Runtime ACL missing: UserProfiles SELECT';
    ELSIF NOT has_table_privilege('rass_app_runtime_v2', 'app."UserProfiles"', 'INSERT') THEN
        RAISE EXCEPTION 'Runtime ACL missing: UserProfiles INSERT';
    ELSIF NOT has_table_privilege('rass_app_runtime_v2', 'app."UserProfiles"', 'UPDATE') THEN
        RAISE EXCEPTION 'Runtime ACL missing: UserProfiles UPDATE';
    ELSIF has_table_privilege('rass_app_runtime_v2', 'app."__EFMigrationsHistory"', 'SELECT') THEN
        RAISE EXCEPTION 'Runtime ACL excessive: __EFMigrationsHistory SELECT';
    ELSIF has_function_privilege('rass_app_runtime_v2', 'app.platform_dashboard()', 'EXECUTE')
       OR has_function_privilege('rass_app_runtime_v2', 'app.platform_roles(text)', 'EXECUTE')
       OR has_function_privilege('rass_app_runtime_v2', 'app.platform_grant_superadmin(uuid,uuid,text,text,text,text)', 'EXECUTE')
       OR has_function_privilege('rass_app_runtime_v2', 'app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)', 'EXECUTE') THEN
        RAISE EXCEPTION 'Runtime ACL excessive: administrative function EXECUTE';
    END IF;
END $$;

SELECT c.relname, c.relrowsecurity, c.relforcerowsecurity
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'app'
  AND c.relname IN ('TenantMemberships', 'UserSessions', 'AuditEntries', 'UserProfiles')
ORDER BY c.relname;

SELECT has_table_privilege('rass_app_runtime_v2', 'app."TenantMemberships"', 'SELECT') AS memberships_select,
       has_table_privilege('rass_app_runtime_v2', 'app."TenantMemberships"', 'INSERT') AS memberships_insert,
       has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'SELECT') AS sessions_select,
       has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'INSERT') AS sessions_insert,
       has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'UPDATE') AS sessions_update,
       has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'DELETE') AS sessions_delete,
       has_table_privilege('rass_app_runtime_v2', 'app."AuditEntries"', 'INSERT') AS audit_insert,
       has_table_privilege('rass_app_runtime_v2', 'app."__EFMigrationsHistory"', 'SELECT') AS ef_history_select;
