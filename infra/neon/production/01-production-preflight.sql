-- Checkpoint 14: Production preflight. READ ONLY.
-- Run only after confirming the Neon Console shows br-lucky-paper-b6egkr8q / neondb.
SELECT current_database() AS database_name, session_user, current_user;

SELECT "MigrationId", "ProductVersion"
FROM app."__EFMigrationsHistory"
ORDER BY "MigrationId";

SELECT rolname, rolcanlogin, rolinherit, rolsuper, rolcreaterole, rolcreatedb,
       rolreplication, rolbypassrls
FROM pg_roles
WHERE rolname IN ('rass_app_runtime_v2', 'rass_platform_admin_v1',
                  'rass_platform_executor_owner_v1')
ORDER BY rolname;

SELECT n.nspname AS schema_name, c.relname AS table_name,
       c.relrowsecurity AS rls_enabled, c.relforcerowsecurity AS force_rls
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'app'
  AND c.relname IN ('Tenants', 'UserProfiles', 'UserSessions', 'TenantMemberships',
                    'AuditEntries', 'CreditCardPurchases', 'CreditCardInvoices',
                    'CreditCardInstallments', 'PlatformRoleAssignments')
ORDER BY c.relname;

SELECT has_schema_privilege('rass_app_runtime_v2', 'app', 'USAGE') AS runtime_usage,
       has_schema_privilege('rass_app_runtime_v2', 'app', 'CREATE') AS runtime_create,
       has_table_privilege('rass_app_runtime_v2', 'app."Tenants"', 'DELETE') AS runtime_tenant_delete,
       has_table_privilege('rass_app_runtime_v2', 'app."UserProfiles"', 'DELETE') AS runtime_profile_delete,
       has_table_privilege('rass_app_runtime_v2', 'app."__EFMigrationsHistory"', 'SELECT') AS runtime_migration_history_read;
