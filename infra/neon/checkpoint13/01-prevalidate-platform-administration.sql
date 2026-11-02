-- CHECKPOINT 13 / SCRIPT 1 — READ ONLY
-- Run only after the SQL Editor identifies br-flat-rice-b6j99et6 / neondb.
SELECT current_database() AS database_name, current_user AS database_user,
       current_setting('server_version') AS server_version;

SELECT rolname, rolcanlogin, rolinherit, rolsuper, rolcreaterole, rolcreatedb,
       rolreplication, rolbypassrls
FROM pg_roles
WHERE rolname IN ('neondb_owner', 'rass_app_runtime_v2',
                  'rass_platform_admin_v1', 'rass_platform_executor_owner_v1')
ORDER BY rolname;

SELECT member_role.rolname AS member, granted_role.rolname AS granted_role,
       membership.admin_option, membership.inherit_option, membership.set_option
FROM pg_auth_members membership
JOIN pg_roles member_role ON member_role.oid = membership.member
JOIN pg_roles granted_role ON granted_role.oid = membership.roleid
WHERE member_role.rolname IN ('neondb_owner', 'rass_app_runtime_v2',
                              'rass_platform_admin_v1', 'rass_platform_executor_owner_v1')
   OR granted_role.rolname IN ('neon_superuser', 'rass_platform_executor_owner_v1')
ORDER BY member, granted_role;

SELECT has_schema_privilege('rass_app_runtime_v2', 'app', 'USAGE') AS runtime_usage,
       has_schema_privilege('rass_app_runtime_v2', 'app', 'CREATE') AS runtime_create,
       has_schema_privilege('rass_platform_admin_v1', 'app', 'USAGE') AS admin_usage,
       has_schema_privilege('rass_platform_admin_v1', 'app', 'CREATE') AS admin_create,
       has_schema_privilege('rass_platform_executor_owner_v1', 'app', 'USAGE') AS executor_usage,
       has_schema_privilege('rass_platform_executor_owner_v1', 'app', 'CREATE') AS executor_create;

SELECT "MigrationId", "ProductVersion" FROM app."__EFMigrationsHistory" ORDER BY "MigrationId";

SELECT c.relname, c.relrowsecurity, c.relforcerowsecurity, pg_get_userbyid(c.relowner) AS owner
FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'app' AND c.relname IN ('PlatformRoleAssignments', 'AuditEntries', 'Tenants', 'UserProfiles')
ORDER BY c.relname;
