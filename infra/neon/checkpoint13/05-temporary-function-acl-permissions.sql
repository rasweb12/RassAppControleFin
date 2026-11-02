-- CHECKPOINT 13 / SCRIPT 5
-- EXECUTAR SOMENTE NA BRANCH DESCARTAVEL, ANTES DA MIGRATION 18.
-- The migration removes precisely this grant with GRANTED BY neondb_owner.
BEGIN;

DO $$
BEGIN
    IF current_database() <> 'neondb' OR current_user <> 'neondb_owner' THEN
        RAISE EXCEPTION 'Unexpected database or migration user';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_platform_executor_owner_v1')
       OR NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_platform_admin_v1')
       OR EXISTS (
        SELECT 1 FROM pg_roles
        WHERE rolname = 'rass_platform_executor_owner_v1'
          AND (rolcanlogin OR rolinherit OR rolsuper OR rolcreaterole OR rolcreatedb OR rolreplication OR rolbypassrls)
    ) OR EXISTS (
        SELECT 1 FROM pg_roles
        WHERE rolname = 'rass_platform_admin_v1'
          AND (NOT rolcanlogin OR rolinherit OR rolsuper OR rolcreaterole OR rolcreatedb OR rolreplication OR rolbypassrls)
    ) OR pg_has_role('rass_platform_executor_owner_v1', 'neon_superuser', 'MEMBER')
      OR pg_has_role('rass_platform_admin_v1', 'neon_superuser', 'MEMBER') THEN
        RAISE EXCEPTION 'Unsafe platform role attributes';
    END IF;
END $$;

-- No schema/table grant is needed for an ACL-only correction.
GRANT rass_platform_executor_owner_v1
TO neondb_owner
WITH INHERIT FALSE, SET TRUE;

COMMIT;
