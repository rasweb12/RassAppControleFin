-- Checkpoint 14: MANUAL, SUPERVISED ROLE PROVISIONING.
-- Do not run from the application, migration runner, or runtime role.
-- First confirm the Neon Console identifies Production branch br-lucky-paper-b6egkr8q / neondb.
-- Stop if the Neon role-management interface cannot guarantee branch scope.

-- Prerequisite roles must be created by the Neon administrative control plane
-- only if it can create them without neon_superuser membership or elevated
-- attributes. If that is not possible, STOP and escalate to Neon support.

ALTER ROLE rass_platform_executor_owner_v1 NOLOGIN NOINHERIT NOSUPERUSER
    NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
ALTER ROLE rass_platform_admin_v1 LOGIN NOINHERIT NOSUPERUSER
    NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
REVOKE neon_superuser FROM rass_platform_executor_owner_v1;
REVOKE neon_superuser FROM rass_platform_admin_v1;

-- Required only through PlatformAdministrationIsolation ownership transfer.
-- PlatformAdministrationHardening removes this grant and CREATE.
GRANT rass_platform_executor_owner_v1 TO neondb_owner
    WITH INHERIT FALSE, SET TRUE;
GRANT USAGE, CREATE ON SCHEMA app TO rass_platform_executor_owner_v1;
GRANT USAGE ON SCHEMA app TO rass_platform_admin_v1;

-- Required again only before PlatformFunctionExecuteHardening; this is a
-- separate maintenance stage and is removed by that migration:
-- GRANT rass_platform_executor_owner_v1 TO neondb_owner
--     WITH INHERIT FALSE, SET TRUE;
