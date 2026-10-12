using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RassAppControleFin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CoreFinanceHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $rls$
                DECLARE target text;
                BEGIN
                  FOREACH target IN ARRAY ARRAY['Accounts','Categories','CreditCards','Transactions','Transfers',
                    'LedgerEntries','CardMovements','SyncOperations','FinancialOutbox']
                  LOOP
                    EXECUTE format('ALTER TABLE app.%I ENABLE ROW LEVEL SECURITY', target);
                    EXECUTE format('ALTER TABLE app.%I FORCE ROW LEVEL SECURITY', target);
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = 'app'
                      AND tablename = target AND policyname = 'finance_user_scope') THEN
                      EXECUTE format('CREATE POLICY finance_user_scope ON app.%I FOR ALL USING (
                        "TenantId" = NULLIF(current_setting(''app.tenant_id'', true), '''')::uuid
                        AND "UserId" = NULLIF(current_setting(''app.user_id'', true), '''')
                      ) WITH CHECK (
                        "TenantId" = NULLIF(current_setting(''app.tenant_id'', true), '''')::uuid
                        AND "UserId" = NULLIF(current_setting(''app.user_id'', true), '''')
                      )', target);
                    END IF;
                  END LOOP;
                END $rls$;

                CREATE OR REPLACE FUNCTION app.assert_transfer_ledger()
                RETURNS trigger LANGUAGE plpgsql SECURITY INVOKER SET search_path = app, pg_temp AS $$
                DECLARE
                  expected_amount numeric(16,2); expected_source uuid; expected_destination uuid;
                  ledger_count integer; ledger_total numeric(16,2);
                  source_delta numeric(16,2); destination_delta numeric(16,2);
                BEGIN
                  SELECT "Amount", "SourceAccountId", "DestinationAccountId"
                    INTO expected_amount, expected_source, expected_destination FROM app."Transfers"
                    WHERE "TenantId" = NEW."TenantId" AND "UserId" = NEW."UserId" AND "Id" = NEW."TransferId";
                  SELECT count(*), coalesce(sum("Delta"), 0),
                    coalesce(sum("Delta") FILTER (WHERE "AccountId" = expected_source), 0),
                    coalesce(sum("Delta") FILTER (WHERE "AccountId" = expected_destination), 0)
                    INTO ledger_count, ledger_total, source_delta, destination_delta FROM app."LedgerEntries"
                    WHERE "TenantId" = NEW."TenantId" AND "UserId" = NEW."UserId" AND "TransferId" = NEW."TransferId";
                  IF ledger_count <> 2 OR ledger_total <> 0 OR source_delta <> -expected_amount
                    OR destination_delta <> expected_amount THEN
                    RAISE EXCEPTION 'Transfer ledger must be a balanced debit and credit' USING ERRCODE = '23514';
                  END IF;
                  RETURN NULL;
                END $$;
                DO $trigger$
                BEGIN
                  IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgrelid = 'app."LedgerEntries"'::regclass
                    AND tgname = 'ck_transfer_ledger_balanced' AND NOT tgisinternal) THEN
                    CREATE CONSTRAINT TRIGGER ck_transfer_ledger_balanced AFTER INSERT OR UPDATE ON app."LedgerEntries"
                      DEFERRABLE INITIALLY DEFERRED FOR EACH ROW WHEN (NEW."TransferId" IS NOT NULL)
                      EXECUTE FUNCTION app.assert_transfer_ledger();
                  END IF;
                END $trigger$;
                GRANT SELECT, INSERT, UPDATE ON app."Accounts", app."Categories", app."CreditCards",
                  app."Transactions" TO rass_app_runtime;
                GRANT SELECT, INSERT ON app."Transfers", app."LedgerEntries", app."CardMovements",
                  app."SyncOperations", app."FinancialOutbox" TO rass_app_runtime;
                REVOKE ALL ON FUNCTION app.assert_transfer_ledger() FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION app.assert_transfer_ledger() TO rass_app_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
