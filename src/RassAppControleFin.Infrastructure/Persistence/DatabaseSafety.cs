using Microsoft.EntityFrameworkCore;

namespace RassAppControleFin.Infrastructure.Persistence;

internal static class DatabaseSafety
{
    internal const string ExpectedMigration = "PlanningAndDashboard";

    internal static async Task<bool> VerifyAsync(AppDbContext db, string runtimeRole,
        CancellationToken cancellationToken)
    {
        // Ownership, privileged memberships and TRUNCATE can defeat row isolation.
        return await db.Database.SqlQuery<bool>($"""
            SELECT (
                current_user = {runtimeRole}
                AND NOT EXISTS (
                    SELECT 1 FROM pg_roles r
                    WHERE pg_has_role(current_user, r.oid, 'MEMBER')
                      AND (r.rolsuper OR r.rolbypassrls OR r.rolcreaterole OR r.rolcreatedb OR r.rolreplication)
                )
                AND NOT has_schema_privilege(current_user, 'app', 'CREATE')
                AND (SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
                    WHERE n.nspname = 'app' AND c.relname IN ('Tenants', 'UserProfiles')
                      AND c.relrowsecurity AND c.relforcerowsecurity
                      AND NOT pg_has_role(current_user, c.relowner, 'MEMBER')) = 2
                AND (SELECT count(*) FROM pg_policies WHERE schemaname = 'app'
                    AND cmd = 'ALL' AND (
                        (tablename = 'Tenants' AND policyname = 'tenant_scope') OR
                        (tablename = 'UserProfiles' AND policyname = 'tenant_user_scope'))) = 2
                AND NOT has_table_privilege(current_user, 'app."Tenants"', 'TRUNCATE')
                AND NOT has_table_privilege(current_user, 'app."UserProfiles"', 'TRUNCATE')
                AND EXISTS (SELECT 1 FROM app."__EFMigrationsHistory"
                    WHERE "MigrationId" LIKE {'%' + ExpectedMigration})
                AND (SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                    WHERE n.nspname='app' AND c.relname IN ('TenantMemberships','UserSessions','AuditEntries',
                      'Accounts','Categories','CreditCards','Transactions','Transfers','LedgerEntries',
                      'CardMovements','SyncOperations','FinancialOutbox','Budgets','Goals')
                    AND c.relrowsecurity AND c.relforcerowsecurity
                    AND NOT pg_has_role(current_user,c.relowner,'MEMBER')
                    AND NOT has_table_privilege(current_user,c.oid,'TRUNCATE'))=14
            ) AS "Value"
            """).SingleAsync(cancellationToken);
    }
}
