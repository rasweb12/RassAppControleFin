-- CHECKPOINT 13 / SCRIPT 3 — READ ONLY AFTER MIGRATIONS 11–17
SELECT "MigrationId", "ProductVersion" FROM app."__EFMigrationsHistory" ORDER BY "MigrationId";

SELECT p.proname, p.prosecdef, pg_get_userbyid(p.proowner) AS owner, p.proconfig
FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace
WHERE n.nspname = 'app' AND p.proname IN
 ('platform_dashboard', 'platform_roles', 'platform_grant_superadmin', 'platform_revoke_superadmin')
ORDER BY p.proname;

SELECT routine_name, grantee, privilege_type
FROM information_schema.routine_privileges
WHERE specific_schema = 'app' AND routine_name IN
 ('platform_dashboard', 'platform_roles', 'platform_grant_superadmin', 'platform_revoke_superadmin')
ORDER BY routine_name, grantee;

SELECT policyname, tablename, roles, cmd
FROM pg_policies
WHERE schemaname = 'app' AND tablename IN ('PlatformRoleAssignments', 'AuditEntries', 'Tenants', 'UserProfiles')
ORDER BY tablename, policyname;

SELECT c.relname, c.relrowsecurity, c.relforcerowsecurity
FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'app' AND c.relname IN ('PlatformRoleAssignments', 'AuditEntries', 'Tenants', 'UserProfiles')
ORDER BY c.relname;

SELECT has_schema_privilege('rass_platform_executor_owner_v1', 'app', 'CREATE') AS executor_can_create,
       pg_has_role('neondb_owner', 'rass_platform_executor_owner_v1', 'MEMBER') AS owner_is_member,
       pg_has_role('neondb_owner', 'rass_platform_executor_owner_v1', 'SET') AS owner_can_set_executor,
       pg_has_role('neondb_owner', 'rass_platform_executor_owner_v1', 'USAGE') AS owner_inherits_executor,
       has_function_privilege('rass_app_runtime_v2', 'app.platform_dashboard()', 'EXECUTE') AS runtime_can_execute_dashboard,
       has_function_privilege('rass_app_runtime_v2', 'app.platform_roles(text)', 'EXECUTE') AS runtime_can_execute_roles,
       has_function_privilege('rass_app_runtime_v2', 'app.platform_grant_superadmin(uuid,uuid,text,text,text,text)', 'EXECUTE') AS runtime_can_execute_grant,
       has_function_privilege('rass_app_runtime_v2', 'app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)', 'EXECUTE') AS runtime_can_execute_revoke,
       has_function_privilege('rass_platform_admin_v1', 'app.platform_dashboard()', 'EXECUTE') AS admin_can_execute_dashboard,
       has_function_privilege('rass_platform_admin_v1', 'app.platform_roles(text)', 'EXECUTE') AS admin_can_execute_roles,
       has_function_privilege('rass_platform_admin_v1', 'app.platform_grant_superadmin(uuid,uuid,text,text,text,text)', 'EXECUTE') AS admin_can_execute_grant,
       has_function_privilege('rass_platform_admin_v1', 'app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)', 'EXECUTE') AS admin_can_execute_revoke;

SELECT p.proname, privilege.privilege_type AS public_privilege
FROM pg_proc p
JOIN pg_namespace n ON n.oid = p.pronamespace
CROSS JOIN LATERAL aclexplode(COALESCE(p.proacl, acldefault('f', p.proowner))) privilege
WHERE n.nspname = 'app'
  AND p.oid IN ('app.platform_dashboard()'::regprocedure,
                'app.platform_roles(text)'::regprocedure,
                'app.platform_grant_superadmin(uuid,uuid,text,text,text,text)'::regprocedure,
                'app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)'::regprocedure)
  AND privilege.grantee = 0
ORDER BY p.proname, privilege.privilege_type;

SELECT EXISTS (
  SELECT 1 FROM pg_auth_members membership
  JOIN pg_roles grantor ON grantor.oid = membership.grantor
  JOIN pg_roles member_role ON member_role.oid = membership.member
  JOIN pg_roles granted_role ON granted_role.oid = membership.roleid
  WHERE grantor.rolname = 'neondb_owner'
    AND member_role.rolname = 'neondb_owner'
    AND granted_role.rolname = 'rass_platform_executor_owner_v1'
) AS temporary_owner_grant_remains;
