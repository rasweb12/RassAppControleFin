using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RassAppControleFin.Infrastructure.Persistence.Migrations;

/// <summary>
/// Restores only the schema traversal privilege required by the financial
/// runtime in databases initialized exclusively through EF migrations.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261102030000_RuntimeSchemaUsageHardening")]
public sealed class RuntimeSchemaUsageHardening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            BEGIN
              IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_app_runtime_v2')
                 OR EXISTS (
                   SELECT 1 FROM pg_roles
                   WHERE rolname = 'rass_app_runtime_v2'
                     AND (rolsuper OR rolbypassrls OR rolcreatedb OR rolcreaterole OR rolreplication OR rolinherit)
                 )
                 OR EXISTS (
                   SELECT 1 FROM pg_auth_members membership
                   JOIN pg_roles child ON child.oid = membership.member
                   WHERE child.rolname = 'rass_app_runtime_v2'
                 ) THEN
                RAISE EXCEPTION 'Unsafe or missing runtime role';
              END IF;
            END $$;

            REVOKE ALL ON SCHEMA app FROM PUBLIC;
            REVOKE CREATE ON SCHEMA app FROM rass_app_runtime_v2;
            GRANT USAGE ON SCHEMA app TO rass_app_runtime_v2;

            DO $$
            BEGIN
              IF NOT has_schema_privilege('rass_app_runtime_v2', 'app', 'USAGE')
                 OR has_schema_privilege('rass_app_runtime_v2', 'app', 'CREATE')
                 OR EXISTS (
                   SELECT 1
                   FROM pg_namespace schema
                   CROSS JOIN LATERAL aclexplode(COALESCE(schema.nspacl, acldefault('n', schema.nspowner))) privilege
                   WHERE schema.nspname = 'app'
                     AND privilege.grantee = 0
                     AND privilege.privilege_type IN ('USAGE', 'CREATE')
                 )
                 OR has_function_privilege('rass_app_runtime_v2', 'app.platform_dashboard()', 'EXECUTE')
                 OR has_function_privilege('rass_app_runtime_v2', 'app.platform_roles(text)', 'EXECUTE')
                 OR has_function_privilege('rass_app_runtime_v2', 'app.platform_grant_superadmin(uuid,uuid,text,text,text,text)', 'EXECUTE')
                 OR has_function_privilege('rass_app_runtime_v2', 'app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)', 'EXECUTE') THEN
                RAISE EXCEPTION 'Runtime schema or administrative function ACL is invalid';
              END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$ BEGIN
              RAISE EXCEPTION 'RuntimeSchemaUsageHardening is forward-only; use a reviewed forward fix.';
            END $$;
            """);
    }
}
