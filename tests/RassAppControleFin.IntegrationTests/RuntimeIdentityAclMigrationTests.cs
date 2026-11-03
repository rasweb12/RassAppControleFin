using Xunit;

namespace RassAppControleFin.IntegrationTests;

public sealed class RuntimeIdentityAclMigrationTests
{
    [Fact]
    public void Identity_acl_migration_grants_only_operations_used_by_identity_store()
    {
        var migration = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "RassAppControleFin.Infrastructure",
            "Persistence", "Migrations", "20261102040000_RuntimeIdentityOperationalAclHardening.cs"));

        Assert.Contains("GRANT SELECT, INSERT, UPDATE ON app.\"UserSessions\" TO rass_app_runtime_v2", migration, StringComparison.Ordinal);
        Assert.Contains("GRANT SELECT, INSERT ON app.\"TenantMemberships\" TO rass_app_runtime_v2", migration, StringComparison.Ordinal);
        Assert.Contains("GRANT SELECT, INSERT ON app.\"AuditEntries\" TO rass_app_runtime_v2", migration, StringComparison.Ordinal);
        Assert.Contains("GRANT SELECT, INSERT ON app.\"Tenants\" TO rass_app_runtime_v2", migration, StringComparison.Ordinal);
        Assert.Contains("GRANT SELECT, INSERT, UPDATE ON app.\"UserProfiles\" TO rass_app_runtime_v2", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("GRANT ALL", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TO PUBLIC", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GRANT SELECT ON app.\"__EFMigrationsHistory\"", migration, StringComparison.Ordinal);
        Assert.Contains("has_function_privilege('rass_app_runtime_v2', 'app.platform_dashboard()', 'EXECUTE')", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Prevalidation_reports_identity_acl_by_object_and_operation()
    {
        var sql = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "infra", "neon", "checkpoint13",
            "12-test-rbac-db-pre-validation.sql"));

        Assert.Contains("Runtime ACL missing: UserSessions INSERT", sql, StringComparison.Ordinal);
        Assert.Contains("Runtime ACL missing: UserSessions UPDATE", sql, StringComparison.Ordinal);
        Assert.Contains("Runtime ACL missing: AuditEntries INSERT", sql, StringComparison.Ordinal);
        Assert.Contains("Runtime ACL excessive: __EFMigrationsHistory SELECT", sql, StringComparison.Ordinal);
        Assert.Contains("Runtime ACL excessive: administrative function EXECUTE", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("GRANT ", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_final_hardening_prestage_revokes_only_legacy_history_read()
    {
        var sql = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "infra", "neon", "production",
            "04-final-hardening-prestage.sql"));

        Assert.Contains("REVOKE SELECT ON TABLE app.\"__EFMigrationsHistory\" FROM rass_app_runtime_v2", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("GRANT ", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TRUNCATE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Monetization_foundation_keeps_paid_offers_disabled_and_runtime_read_only()
    {
        var migration = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "RassAppControleFin.Infrastructure",
            "Persistence", "Migrations", "20261103112705_MonetizationFoundation.cs"));

        Assert.Contains("('PremiumMonthly','Premium Mensal',14.90,'BRL',false", migration, StringComparison.Ordinal);
        Assert.Contains("GRANT SELECT ON app.\"PlanDefinitions\", app.\"PlanEntitlements\", app.\"UserSubscriptions\" TO rass_app_runtime_v2", migration, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE app.\"PaymentEvents\" FORCE ROW LEVEL SECURITY", migration, StringComparison.Ordinal);
        Assert.Contains("CREATE POLICY subscription_user_scope", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("GRANT ALL", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TO PUBLIC", migration, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props"))
                && File.Exists(Path.Combine(directory.FullName, "RassAppControleFin.slnx")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
