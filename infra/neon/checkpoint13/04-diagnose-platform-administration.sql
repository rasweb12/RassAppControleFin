-- CHECKPOINT 13 / SCRIPT 4 — READ ONLY DIAGNOSTICS
SELECT d.classid::regclass AS dependent_catalog, d.objid::regclass AS dependent_object,
       d.refobjid::regrole AS referenced_role, d.deptype
FROM pg_shdepend d JOIN pg_roles r ON r.oid = d.refobjid
WHERE r.rolname IN ('rass_platform_admin_v1', 'rass_platform_executor_owner_v1')
ORDER BY referenced_role;

SELECT usename, application_name, state, backend_type
FROM pg_stat_activity
WHERE usename IN ('rass_platform_admin_v1', 'rass_platform_executor_owner_v1');

SELECT table_name, grantee, privilege_type
FROM information_schema.role_table_grants
WHERE table_schema = 'app'
  AND table_name IN ('PlatformRoleAssignments', 'AuditEntries', 'Tenants', 'UserProfiles')
ORDER BY table_name, grantee, privilege_type;
