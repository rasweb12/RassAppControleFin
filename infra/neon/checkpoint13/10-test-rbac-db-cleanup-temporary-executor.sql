-- CHECKPOINT 13 / RBAC TEST DATABASE ONLY
-- Safe to run after either a successful or failed test execution.
-- Removes only the membership granted by neondb_owner; independent grants survive.
BEGIN;

DO $$
BEGIN
    IF current_database() <> 'rass_sprint1_tests' OR current_user <> 'neondb_owner' THEN
        RAISE EXCEPTION 'Unexpected database or cleanup user';
    END IF;
END $$;

REVOKE CREATE ON SCHEMA app FROM rass_platform_executor_owner_v1;
REVOKE rass_platform_executor_owner_v1 FROM neondb_owner GRANTED BY neondb_owner;

DO $$
BEGIN
    IF has_schema_privilege('rass_platform_executor_owner_v1', 'app', 'CREATE')
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
             AND granted_role.rolname = 'rass_platform_executor_owner_v1') THEN
        RAISE EXCEPTION 'Temporary platform executor privilege remains';
    END IF;
END $$;

COMMIT;
