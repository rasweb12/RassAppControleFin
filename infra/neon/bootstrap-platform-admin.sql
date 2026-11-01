-- Run manually as the isolated/production migration owner, never as the runtime.
-- Supply the password through the Neon console or an approved secret manager.
-- The executor owner cannot log in. Only the migration owner receives this
-- membership, solely so it can transfer ownership of the fixed functions.
CREATE ROLE rass_platform_executor_owner_v1 NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
CREATE ROLE rass_platform_admin_v1 LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD '<set-outside-git>';
GRANT CONNECT ON DATABASE rass_sprint1_tests TO rass_platform_admin_v1;
GRANT USAGE ON SCHEMA app TO rass_platform_admin_v1;
GRANT rass_platform_executor_owner_v1 TO neondb_owner;
GRANT CREATE ON SCHEMA app TO rass_platform_executor_owner_v1;

-- Table grants, RLS policies, function ownership and EXECUTE permissions are
-- intentionally applied by 20261101100000_PlatformAdministrationIsolation.
