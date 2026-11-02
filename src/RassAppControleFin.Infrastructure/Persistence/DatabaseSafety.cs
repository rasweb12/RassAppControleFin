using Microsoft.EntityFrameworkCore;

namespace RassAppControleFin.Infrastructure.Persistence;

internal static class DatabaseSafety
{
    // The API maps these account-classification fields. Checking their presence
    // through PostgreSQL catalogs keeps readiness independent of EF's migration
    // history table, which is deployment metadata rather than runtime data.
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
                AND has_table_privilege(current_user, 'app."TenantMemberships"', 'SELECT')
                AND has_table_privilege(current_user, 'app."TenantMemberships"', 'INSERT')
                AND has_table_privilege(current_user, 'app."UserSessions"', 'SELECT')
                AND has_table_privilege(current_user, 'app."UserSessions"', 'INSERT')
                AND has_table_privilege(current_user, 'app."UserSessions"', 'UPDATE')
                AND has_table_privilege(current_user, 'app."AuditEntries"', 'INSERT')
                AND has_table_privilege(current_user, 'app."Tenants"', 'SELECT')
                AND has_table_privilege(current_user, 'app."Tenants"', 'INSERT')
                AND has_table_privilege(current_user, 'app."UserProfiles"', 'SELECT')
                AND has_table_privilege(current_user, 'app."UserProfiles"', 'INSERT')
                AND has_table_privilege(current_user, 'app."UserProfiles"', 'UPDATE')
                AND NOT has_table_privilege(current_user, 'app."__EFMigrationsHistory"', 'SELECT')
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
                AND (SELECT count(*) FROM pg_attribute attribute
                    JOIN pg_class table_class ON table_class.oid = attribute.attrelid
                    JOIN pg_namespace schema ON schema.oid = table_class.relnamespace
                    WHERE schema.nspname = 'app'
                      AND table_class.relname = 'Accounts'
                      AND attribute.attnum > 0
                      AND NOT attribute.attisdropped
                      AND attribute.attname IN ('IncludeInNetWorth', 'Institution', 'Subtype', 'Type')) = 4
                AND (SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                    WHERE n.nspname='app' AND c.relname IN ('TenantMemberships','UserSessions','AuditEntries',
                      'Accounts','Categories','CreditCards','Transactions','Transfers','LedgerEntries',
                      'CardMovements','CreditCardPurchases','CreditCardInvoices','CreditCardInstallments','SyncOperations','FinancialOutbox','Budgets','Goals','ExpenseReductionGoals','FinancialNotifications',
                      'PayYourselfFirstPlans','PayYourselfFirstPlanRevisions','EmergencyFundPlans','InvestmentAssets',
                      'InvestmentTransactions','TargetPortfolioAllocations')
                    AND c.relrowsecurity AND c.relforcerowsecurity
                    AND NOT pg_has_role(current_user,c.relowner,'MEMBER')
                    AND NOT has_table_privilege(current_user,c.oid,'TRUNCATE'))=25
            ) AS "Value"
            """).SingleAsync(cancellationToken);
    }

    internal static async Task<string> DiagnoseNotReadyAsync(AppDbContext db,
        CancellationToken cancellationToken)
    {
        var schemaExists = await db.Database.SqlQuery<bool>($"""
            SELECT EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'app') AS "Value"
            """).SingleAsync(cancellationToken);

        if (!schemaExists)
            return "database.schema_missing";

        var migrationTableExists = await db.Database.SqlQuery<bool>($"""
            SELECT to_regclass('app."__EFMigrationsHistory"') IS NOT NULL AS "Value"
            """).SingleAsync(cancellationToken);

        if (!migrationTableExists)
            return "database.migration_pending";

        var requiredTablesPresent = await db.Database.SqlQuery<bool>($"""
            SELECT count(*) = 27 AS "Value"
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = 'app'
              AND c.relname IN (
                  'TenantMemberships', 'UserSessions', 'AuditEntries', 'Accounts', 'Categories',
                  'CreditCards', 'Transactions', 'Transfers', 'LedgerEntries', 'CardMovements', 'CreditCardPurchases', 'CreditCardInvoices', 'CreditCardInstallments',
                  'SyncOperations', 'FinancialOutbox', 'Budgets', 'Goals', 'ExpenseReductionGoals', 'FinancialNotifications',
                  'PayYourselfFirstPlans', 'PayYourselfFirstPlanRevisions', 'EmergencyFundPlans', 'InvestmentAssets',
                  'InvestmentTransactions', 'TargetPortfolioAllocations', 'Tenants', 'UserProfiles')
              AND c.relkind IN ('r', 'p')
            """).SingleAsync(cancellationToken);

        if (!requiredTablesPresent)
            return "database.migration_pending";

        var expectedSchemaPresent = await db.Database.SqlQuery<bool>($"""
            SELECT count(*) = 4 AS "Value"
            FROM pg_attribute attribute
            JOIN pg_class table_class ON table_class.oid = attribute.attrelid
            JOIN pg_namespace schema ON schema.oid = table_class.relnamespace
            WHERE schema.nspname = 'app'
              AND table_class.relname = 'Accounts'
              AND attribute.attnum > 0
              AND NOT attribute.attisdropped
              AND attribute.attname IN ('IncludeInNetWorth', 'Institution', 'Subtype', 'Type')
            """).SingleAsync(cancellationToken);

        return expectedSchemaPresent ? "database.rls_invalid" : "database.migration_pending";
    }
}
