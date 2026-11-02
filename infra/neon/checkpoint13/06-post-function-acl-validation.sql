-- CHECKPOINT 13 / SCRIPT 6 — READ ONLY AFTER MIGRATION 18
SELECT "MigrationId", "ProductVersion"
FROM app."__EFMigrationsHistory"
WHERE "MigrationId" = '20261102020000_PlatformFunctionExecuteHardening';

SELECT p.proname,
       p.prosecdef AS security_definer,
       pg_get_userbyid(p.proowner) AS owner,
       p.proconfig,
       has_function_privilege('rass_app_runtime_v2', p.oid, 'EXECUTE') AS runtime_can_execute,
       has_function_privilege('rass_platform_admin_v1', p.oid, 'EXECUTE') AS admin_can_execute,
       EXISTS (
         SELECT 1 FROM aclexplode(COALESCE(p.proacl, acldefault('f', p.proowner))) privilege
         WHERE privilege.grantee = 0 AND privilege.privilege_type = 'EXECUTE'
       ) AS public_can_execute
FROM pg_proc p
JOIN pg_namespace n ON n.oid = p.pronamespace
WHERE n.nspname = 'app'
  AND p.oid IN ('app.platform_dashboard()'::regprocedure,
                'app.platform_roles(text)'::regprocedure,
                'app.platform_grant_superadmin(uuid,uuid,text,text,text,text)'::regprocedure,
                'app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)'::regprocedure)
ORDER BY p.proname;

SELECT has_schema_privilege('rass_platform_executor_owner_v1', 'app', 'CREATE') AS executor_can_create,
       pg_has_role('neondb_owner', 'rass_platform_executor_owner_v1', 'SET') AS owner_can_set_executor,
       pg_has_role('neondb_owner', 'rass_platform_executor_owner_v1', 'USAGE') AS owner_inherits_executor,
       EXISTS (
         SELECT 1
         FROM pg_auth_members membership
         JOIN pg_roles grantor ON grantor.oid = membership.grantor
         JOIN pg_roles member_role ON member_role.oid = membership.member
         JOIN pg_roles granted_role ON granted_role.oid = membership.roleid
         WHERE grantor.rolname = 'neondb_owner'
           AND member_role.rolname = 'neondb_owner'
           AND granted_role.rolname = 'rass_platform_executor_owner_v1'
       ) AS temporary_owner_grant_remains;
