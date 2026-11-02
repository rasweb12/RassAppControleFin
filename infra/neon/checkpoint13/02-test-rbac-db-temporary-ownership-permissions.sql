-- CHECKPOINT 13 / RBAC TEST DATABASE ONLY
-- Run after migrations 1-15 and before migration 16 in rass_sprint1_tests.
-- This is intentionally separate from 02-temporary-ownership-permissions.sql.
BEGIN;

DO $$
BEGIN
    IF current_database() <> 'rass_sprint1_tests' OR current_user <> 'neondb_owner' THEN
        RAISE EXCEPTION 'Unexpected database or migration user';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_platform_executor_owner_v1')
       OR NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_platform_admin_v1')
       OR NOT EXISTS (
           SELECT 1 FROM app."__EFMigrationsHistory"
           WHERE "MigrationId" = '20261101052030_PlatformRbacFoundation')
       OR EXISTS (
           SELECT 1 FROM app."__EFMigrationsHistory"
           WHERE "MigrationId" = '20261101100000_PlatformAdministrationIsolation') THEN
        RAISE EXCEPTION 'Unexpected migration or platform-role prerequisite';
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

-- Only migration 16 requires these privileges. Migration 17 removes CREATE
-- and the direct grant made by neondb_owner.
GRANT rass_platform_executor_owner_v1
TO neondb_owner
WITH INHERIT FALSE, SET TRUE;
GRANT USAGE, CREATE ON SCHEMA app TO rass_platform_executor_owner_v1;
GRANT USAGE ON SCHEMA app TO rass_platform_admin_v1;

COMMIT;
