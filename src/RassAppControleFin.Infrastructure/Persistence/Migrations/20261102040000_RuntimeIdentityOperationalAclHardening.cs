using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RassAppControleFin.Infrastructure.Persistence.Migrations;

/// <summary>
/// Makes the minimum identity/session ACLs reproducible for databases created
/// exclusively from EF migrations. Row access continues to be constrained by
/// the existing RLS and FORCE RLS policies.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261102040000_RuntimeIdentityOperationalAclHardening")]
public sealed class RuntimeIdentityOperationalAclHardening : Migration
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

            REVOKE ALL ON app."TenantMemberships", app."UserSessions", app."AuditEntries",
                          app."Tenants", app."UserProfiles" FROM PUBLIC;
            REVOKE ALL ON app."TenantMemberships", app."UserSessions", app."AuditEntries",
                          app."Tenants", app."UserProfiles" FROM rass_app_runtime_v2;

            -- IdentityStore reads and creates memberships; membership changes
            -- remain protected by its RLS insert policy and are never mutable
            -- by the financial runtime.
            GRANT SELECT, INSERT ON app."TenantMemberships" TO rass_app_runtime_v2;
            -- Session renewal/revocation uses predicates and therefore needs
            -- SELECT in addition to INSERT and UPDATE. DELETE is intentionally
            -- absent so session history cannot be erased by the runtime.
            GRANT SELECT, INSERT, UPDATE ON app."UserSessions" TO rass_app_runtime_v2;
            -- Runtime audit is append-only; SELECT supports the caller's own
            -- audited history under audit_read RLS, without mutation rights.
            GRANT SELECT, INSERT ON app."AuditEntries" TO rass_app_runtime_v2;
            -- Personal-tenant onboarding only creates and reads tenants.
            GRANT SELECT, INSERT ON app."Tenants" TO rass_app_runtime_v2;
            -- Profile reads, creation and optimistic-concurrency updates are
            -- required by IdentityStore; physical deletion is deliberately not.
            GRANT SELECT, INSERT, UPDATE ON app."UserProfiles" TO rass_app_runtime_v2;

            DO $$
            BEGIN
              IF NOT has_table_privilege('rass_app_runtime_v2', 'app."TenantMemberships"', 'SELECT')
                 OR NOT has_table_privilege('rass_app_runtime_v2', 'app."TenantMemberships"', 'INSERT')
                 OR has_table_privilege('rass_app_runtime_v2', 'app."TenantMemberships"', 'UPDATE')
                 OR has_table_privilege('rass_app_runtime_v2', 'app."TenantMemberships"', 'DELETE')
                 OR NOT has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'SELECT')
                 OR NOT has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'INSERT')
                 OR NOT has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'UPDATE')
                 OR has_table_privilege('rass_app_runtime_v2', 'app."UserSessions"', 'DELETE')
                 OR NOT has_table_privilege('rass_app_runtime_v2', 'app."AuditEntries"', 'SELECT')
                 OR NOT has_table_privilege('rass_app_runtime_v2', 'app."AuditEntries"', 'INSERT')
                 OR has_table_privilege('rass_app_runtime_v2', 'app."AuditEntries"', 'UPDATE')
                 OR has_table_privilege('rass_app_runtime_v2', 'app."AuditEntries"', 'DELETE')
                 OR NOT has_table_privilege('rass_app_runtime_v2', 'app."Tenants"', 'SELECT')
                 OR NOT has_table_privilege('rass_app_runtime_v2', 'app."Tenants"', 'INSERT')
                 OR has_table_privilege('rass_app_runtime_v2', 'app."Tenants"', 'UPDATE')
                 OR has_table_privilege('rass_app_runtime_v2', 'app."Tenants"', 'DELETE')
                 OR NOT has_table_privilege('rass_app_runtime_v2', 'app."UserProfiles"', 'SELECT')
                 OR NOT has_table_privilege('rass_app_runtime_v2', 'app."UserProfiles"', 'INSERT')
                 OR NOT has_table_privilege('rass_app_runtime_v2', 'app."UserProfiles"', 'UPDATE')
                 OR has_table_privilege('rass_app_runtime_v2', 'app."UserProfiles"', 'DELETE')
                 OR has_table_privilege('rass_app_runtime_v2', 'app."__EFMigrationsHistory"', 'SELECT')
                 OR has_schema_privilege('rass_app_runtime_v2', 'app', 'CREATE')
                 OR has_function_privilege('rass_app_runtime_v2', 'app.platform_dashboard()', 'EXECUTE')
                 OR has_function_privilege('rass_app_runtime_v2', 'app.platform_roles(text)', 'EXECUTE')
                 OR has_function_privilege('rass_app_runtime_v2', 'app.platform_grant_superadmin(uuid,uuid,text,text,text,text)', 'EXECUTE')
                 OR has_function_privilege('rass_app_runtime_v2', 'app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)', 'EXECUTE') THEN
                RAISE EXCEPTION 'Runtime identity operational ACL is invalid';
              END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$ BEGIN
              RAISE EXCEPTION 'RuntimeIdentityOperationalAclHardening is forward-only; use a reviewed forward fix.';
            END $$;
            """);
    }
}
