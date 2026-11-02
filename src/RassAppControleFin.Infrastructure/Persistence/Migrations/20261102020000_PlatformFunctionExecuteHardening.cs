using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RassAppControleFin.Infrastructure.Persistence.Migrations;

/// <summary>
/// Corrects function ACLs after the executor became the owner. The migration
/// temporarily assumes the NOLOGIN owner only to change its own function ACLs
/// and removes that capability before committing.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261102020000_PlatformFunctionExecuteHardening")]
public sealed class PlatformFunctionExecuteHardening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            BEGIN
              IF current_user <> 'neondb_owner'
                 OR NOT pg_has_role(current_user, 'rass_platform_executor_owner_v1', 'SET') THEN
                RAISE EXCEPTION 'Migration owner must temporarily have SET permission for the platform executor';
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
                RAISE EXCEPTION 'Platform administration roles are not safely provisioned';
              END IF;

              IF (SELECT count(*)
                  FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace
                  WHERE n.nspname = 'app'
                    AND p.oid IN (
                      'app.platform_dashboard()'::regprocedure,
                      'app.platform_roles(text)'::regprocedure,
                      'app.platform_grant_superadmin(uuid,uuid,text,text,text,text)'::regprocedure,
                      'app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)'::regprocedure)
                    AND p.prosecdef
                    AND pg_get_userbyid(p.proowner) = 'rass_platform_executor_owner_v1'
                    AND p.proconfig @> ARRAY['search_path=pg_catalog, app']) <> 4 THEN
                RAISE EXCEPTION 'Platform function security definition, owner, or search path is invalid';
              END IF;
            END $$;

            SET LOCAL ROLE rass_platform_executor_owner_v1;
            REVOKE ALL ON FUNCTION app.platform_dashboard(), app.platform_roles(text),
              app.platform_grant_superadmin(uuid,uuid,text,text,text,text),
              app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)
              FROM PUBLIC, rass_app_runtime_v2;
            GRANT EXECUTE ON FUNCTION app.platform_dashboard(), app.platform_roles(text),
              app.platform_grant_superadmin(uuid,uuid,text,text,text,text),
              app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)
              TO rass_platform_admin_v1;
            RESET ROLE;

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
                      AND granted_role.rolname = 'rass_platform_executor_owner_v1')
                 OR EXISTS (
                    SELECT 1
                    FROM pg_proc p
                    JOIN pg_namespace n ON n.oid = p.pronamespace
                    CROSS JOIN LATERAL aclexplode(COALESCE(p.proacl, acldefault('f', p.proowner))) privilege
                    WHERE n.nspname = 'app'
                      AND p.oid IN (
                        'app.platform_dashboard()'::regprocedure,
                        'app.platform_roles(text)'::regprocedure,
                        'app.platform_grant_superadmin(uuid,uuid,text,text,text,text)'::regprocedure,
                        'app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)'::regprocedure)
                      AND privilege.grantee = 0
                      AND privilege.privilege_type = 'EXECUTE')
                 OR has_function_privilege('rass_app_runtime_v2', 'app.platform_dashboard()', 'EXECUTE')
                 OR has_function_privilege('rass_app_runtime_v2', 'app.platform_roles(text)', 'EXECUTE')
                 OR has_function_privilege('rass_app_runtime_v2', 'app.platform_grant_superadmin(uuid,uuid,text,text,text,text)', 'EXECUTE')
                 OR has_function_privilege('rass_app_runtime_v2', 'app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)', 'EXECUTE')
                 OR NOT has_function_privilege('rass_platform_admin_v1', 'app.platform_dashboard()', 'EXECUTE')
                 OR NOT has_function_privilege('rass_platform_admin_v1', 'app.platform_roles(text)', 'EXECUTE')
                 OR NOT has_function_privilege('rass_platform_admin_v1', 'app.platform_grant_superadmin(uuid,uuid,text,text,text,text)', 'EXECUTE')
                 OR NOT has_function_privilege('rass_platform_admin_v1', 'app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)', 'EXECUTE') THEN
                RAISE EXCEPTION 'Platform function execution privileges are not safely restricted';
              END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$ BEGIN
              RAISE EXCEPTION 'PlatformFunctionExecuteHardening is forward-only; use a reviewed forward fix.';
            END $$;
            """);
    }
}
