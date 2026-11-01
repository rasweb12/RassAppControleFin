-- Run explicitly as the deployment/migration owner, never as the API.
-- Provision rass_app_runtime_v2 (LOGIN/password) in the provider secret store before running this file.
DO $bootstrap$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_app_runtime_v2') THEN
        RAISE EXCEPTION 'Missing runtime role rass_app_runtime_v2. Provision it outside this bootstrap.';
    END IF;
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_app_runtime_v2'
        AND (rolsuper OR rolbypassrls OR rolcreatedb OR rolcreaterole OR rolreplication OR rolinherit))
        OR EXISTS (SELECT 1 FROM pg_auth_members membership JOIN pg_roles child ON child.oid = membership.member
            WHERE child.rolname = 'rass_app_runtime_v2')
    THEN
        RAISE EXCEPTION 'Unsafe runtime role rass_app_runtime_v2';
    END IF;
END $bootstrap$;
REVOKE ALL ON SCHEMA app FROM PUBLIC;
REVOKE ALL ON SCHEMA app FROM rass_app_runtime_v2;
REVOKE ALL ON ALL TABLES IN SCHEMA app FROM PUBLIC;
REVOKE ALL ON ALL TABLES IN SCHEMA app FROM rass_app_runtime_v2;
GRANT USAGE ON SCHEMA app TO rass_app_runtime_v2;
GRANT SELECT, INSERT, UPDATE, DELETE ON app."Tenants", app."UserProfiles" TO rass_app_runtime_v2;
GRANT SELECT ON app."__EFMigrationsHistory" TO rass_app_runtime_v2;
GRANT SELECT, INSERT ON app."TenantMemberships", app."AuditEntries" TO rass_app_runtime_v2;
GRANT SELECT, INSERT, UPDATE ON app."UserSessions" TO rass_app_runtime_v2;
GRANT SELECT, INSERT, UPDATE ON app."Accounts", app."Categories", app."CreditCards",
    app."Transactions", app."Budgets", app."Goals", app."ExpenseReductionGoals",
    app."FinancialNotifications" TO rass_app_runtime_v2;
GRANT SELECT, INSERT ON app."Transfers", app."LedgerEntries", app."CardMovements",
    app."SyncOperations", app."FinancialOutbox" TO rass_app_runtime_v2;
GRANT SELECT, INSERT, UPDATE ON app."CreditCardPurchases", app."CreditCardInvoices",
    app."CreditCardInstallments" TO rass_app_runtime_v2;
GRANT SELECT, INSERT, UPDATE ON app."PayYourselfFirstPlans", app."EmergencyFundPlans",
    app."InvestmentAssets", app."TargetPortfolioAllocations" TO rass_app_runtime_v2;
GRANT SELECT, INSERT ON app."PayYourselfFirstPlanRevisions", app."InvestmentTransactions" TO rass_app_runtime_v2;
GRANT EXECUTE ON FUNCTION app.assert_transfer_ledger() TO rass_app_runtime_v2;
-- Deliberately no default privileges: each future migration must declare grants.
