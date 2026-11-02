using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RassAppControleFin.Infrastructure.Persistence.Migrations;

/// <summary>
/// Forward-only hardening for the isolated platform executor. This migration
/// deliberately refuses unsafe pre-provisioned roles instead of trying to
/// alter cluster-level attributes from an application migration.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261102010000_PlatformAdministrationHardening")]
public sealed class PlatformAdministrationHardening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            BEGIN
              IF EXISTS (
                SELECT 1 FROM pg_roles
                WHERE rolname = 'rass_platform_executor_owner_v1'
                  AND (rolcanlogin OR rolinherit OR rolsuper OR rolcreaterole OR rolcreatedb OR rolreplication OR rolbypassrls)
              ) OR EXISTS (
                SELECT 1 FROM pg_roles
                WHERE rolname = 'rass_platform_admin_v1'
                  AND (NOT rolcanlogin OR rolinherit OR rolsuper OR rolcreaterole OR rolcreatedb OR rolreplication OR rolbypassrls)
              ) OR pg_has_role('rass_platform_executor_owner_v1', 'neon_superuser', 'member')
                OR pg_has_role('rass_platform_admin_v1', 'neon_superuser', 'member') THEN
                RAISE EXCEPTION 'Platform administration roles are not safely provisioned';
              END IF;
            END $$;

            REVOKE ALL ON app."PlatformRoleAssignments", app."AuditEntries", app."Tenants", app."UserProfiles"
              FROM rass_platform_executor_owner_v1;
            GRANT SELECT, INSERT, UPDATE ON app."PlatformRoleAssignments" TO rass_platform_executor_owner_v1;
            GRANT INSERT ON app."AuditEntries" TO rass_platform_executor_owner_v1;
            GRANT SELECT ON app."Tenants", app."UserProfiles" TO rass_platform_executor_owner_v1;

            -- CREATE and SET are only needed while migration 16 transfers
            -- ownership. PostgreSQL 18 can retain an independent cloud_admin
            -- membership, so revoke only the grant issued by the migration owner.
            REVOKE CREATE ON SCHEMA app FROM rass_platform_executor_owner_v1;
            REVOKE rass_platform_executor_owner_v1 FROM neondb_owner GRANTED BY neondb_owner;

            REVOKE ALL ON FUNCTION app.platform_dashboard(), app.platform_roles(text),
              app.platform_grant_superadmin(uuid,uuid,text,text,text,text),
              app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)
              FROM PUBLIC, rass_app_runtime_v2;
            GRANT EXECUTE ON FUNCTION app.platform_dashboard(), app.platform_roles(text),
              app.platform_grant_superadmin(uuid,uuid,text,text,text,text),
              app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)
              TO rass_platform_admin_v1;

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
                      AND granted_role.rolname = 'rass_platform_executor_owner_v1'
                 ) THEN
                RAISE EXCEPTION 'Temporary platform executor privileges were not removed';
              END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$ BEGIN
              RAISE EXCEPTION 'PlatformAdministrationHardening is forward-only; use a reviewed forward fix.';
            END $$;
            """);
    }
}
