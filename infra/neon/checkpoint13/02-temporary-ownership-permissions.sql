
-- CHECKPOINT 13 / SCRIPT 2
-- EXECUTAR SOMENTE NA BRANCH DESCARTAVEL
-- validate-checkpoint13-production-upgrade
-- Branch ID: br-flat-rice-b6j99et6

BEGIN;

DO $$
BEGIN
    IF current_database() <> 'neondb'
       OR current_user <> 'neondb_owner' THEN
        RAISE EXCEPTION 'Unexpected database or user';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_roles
        WHERE rolname = 'rass_platform_executor_owner_v1'
    )
    OR NOT EXISTS (
        SELECT 1 FROM pg_roles
        WHERE rolname = 'rass_platform_admin_v1'
    ) THEN
        RAISE EXCEPTION 'Missing platform administration role';
    END IF;

    IF EXISTS (
        SELECT 1 FROM pg_roles
        WHERE rolname = 'rass_platform_executor_owner_v1'
          AND (
              rolcanlogin OR rolinherit OR rolsuper
              OR rolcreaterole OR rolcreatedb
              OR rolreplication OR rolbypassrls
          )
    )
    OR EXISTS (
        SELECT 1 FROM pg_roles
        WHERE rolname = 'rass_platform_admin_v1'
          AND (
              NOT rolcanlogin OR rolinherit OR rolsuper
              OR rolcreaterole OR rolcreatedb
              OR rolreplication OR rolbypassrls
          )
    )
    OR pg_has_role(
        'rass_platform_executor_owner_v1',
        'neon_superuser',
        'MEMBER'
    )
    OR pg_has_role(
        'rass_platform_admin_v1',
        'neon_superuser',
        'MEMBER'
    ) THEN
        RAISE EXCEPTION 'Unsafe platform role attributes or membership';
    END IF;

    IF NOT pg_has_role(
        'neondb_owner',
        'rass_platform_executor_owner_v1',
        'SET'
    ) THEN
        RAISE EXCEPTION 'Executor SET permission missing';
    END IF;
END $$;

GRANT rass_platform_executor_owner_v1
TO neondb_owner
WITH INHERIT FALSE, SET TRUE;

GRANT USAGE, CREATE ON SCHEMA app
TO rass_platform_executor_owner_v1;

GRANT USAGE ON SCHEMA app
TO rass_platform_admin_v1;

COMMIT;
