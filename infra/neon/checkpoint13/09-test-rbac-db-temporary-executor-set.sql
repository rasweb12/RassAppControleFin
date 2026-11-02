-- CHECKPOINT 13 / RBAC TEST DATABASE ONLY
-- Run only after migration 20 and before PlatformRbacTests.
-- The fixture currently uses SET LOCAL ROLE only to seed synthetic SuperAdmins.
BEGIN;

DO $$
BEGIN
    IF current_database() <> 'rass_sprint1_tests' OR current_user <> 'neondb_owner' THEN
        RAISE EXCEPTION 'Unexpected database or test user';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM app."__EFMigrationsHistory"
        WHERE "MigrationId" = '20261102040000_RuntimeIdentityOperationalAclHardening')
       OR EXISTS (
        SELECT 1 FROM pg_proc p
        JOIN pg_namespace n ON n.oid = p.pronamespace
        CROSS JOIN LATERAL aclexplode(COALESCE(p.proacl, acldefault('f', p.proowner))) privilege
        WHERE n.nspname = 'app'
          AND p.oid IN ('app.platform_dashboard()'::regprocedure,
                        'app.platform_roles(text)'::regprocedure,
                        'app.platform_grant_superadmin(uuid,uuid,text,text,text,text)'::regprocedure,
                        'app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)'::regprocedure)
          AND privilege.grantee = 0
          AND privilege.privilege_type = 'EXECUTE') THEN
        RAISE EXCEPTION 'Migration 20 or function ACL prerequisite is missing';
    END IF;
END $$;

-- No CREATE, table grant, inheritance, or runtime grant is added.
GRANT rass_platform_executor_owner_v1
TO neondb_owner
WITH INHERIT FALSE, SET TRUE;

COMMIT;
