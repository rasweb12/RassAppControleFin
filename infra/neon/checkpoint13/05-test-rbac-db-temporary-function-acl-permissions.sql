-- CHECKPOINT 13 / RBAC TEST DATABASE ONLY
-- Run after migration 17 and before migration 18 in rass_sprint1_tests.
-- This is intentionally separate from 05-temporary-function-acl-permissions.sql.
BEGIN;

DO $$
BEGIN
    IF current_database() <> 'rass_sprint1_tests' OR current_user <> 'neondb_owner' THEN
        RAISE EXCEPTION 'Unexpected database or migration user';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM app."__EFMigrationsHistory"
        WHERE "MigrationId" = '20261102010000_PlatformAdministrationHardening')
       OR EXISTS (
        SELECT 1 FROM app."__EFMigrationsHistory"
        WHERE "MigrationId" = '20261102020000_PlatformFunctionExecuteHardening') THEN
        RAISE EXCEPTION 'Unexpected migration prerequisite for function ACL hardening';
    END IF;

    IF EXISTS (
        SELECT 1 FROM pg_roles
        WHERE rolname = 'rass_platform_executor_owner_v1'
          AND (rolcanlogin OR rolinherit OR rolsuper OR rolcreaterole OR rolcreatedb OR rolreplication OR rolbypassrls)
    ) OR EXISTS (
        SELECT 1 FROM pg_roles
        WHERE rolname = 'rass_platform_admin_v1'
          AND (NOT rolcanlogin OR rolinherit OR rolsuper OR rolcreaterole OR rolcreatedb OR rolreplication OR rolbypassrls)
    ) OR pg_has_role('rass_platform_executor_owner_v1', 'neon_superuser', 'MEMBER')
      OR pg_has_role('rass_platform_admin_v1', 'neon_superuser', 'MEMBER') THEN
        RAISE EXCEPTION 'Unsafe platform role attributes or membership';
    END IF;
END $$;

-- No schema or table privilege is required for this ACL-only correction.
GRANT rass_platform_executor_owner_v1
TO neondb_owner
WITH INHERIT FALSE, SET TRUE;

COMMIT;
