DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'app') THEN
        CREATE SCHEMA app;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS app."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'app') THEN
        CREATE SCHEMA app;
    END IF;
END $EF$;

CREATE TABLE app."Tenants" (
    "Id" uuid NOT NULL,
    "Name" character varying(120) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Tenants" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Tenants_Name" CHECK (length(btrim("Name")) BETWEEN 1 AND 120)
);

CREATE TABLE app."UserProfiles" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" character varying(256) NOT NULL,
    "DisplayName" character varying(120) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_UserProfiles" PRIMARY KEY ("TenantId", "Id"),
    CONSTRAINT "CK_UserProfiles_DisplayName" CHECK (length(btrim("DisplayName")) BETWEEN 1 AND 120),
    CONSTRAINT "CK_UserProfiles_UserId" CHECK (length(btrim("UserId")) BETWEEN 1 AND 256),
    CONSTRAINT "CK_UserProfiles_Version" CHECK ("Version" > 0),
    CONSTRAINT "FK_UserProfiles_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES app."Tenants" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_UserProfiles_TenantId_UserId" ON app."UserProfiles" ("TenantId", "UserId");

ALTER TABLE app."Tenants" ENABLE ROW LEVEL SECURITY;
ALTER TABLE app."Tenants" FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_scope ON app."Tenants" FOR ALL TO PUBLIC
    USING ("Id" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
        AND NULLIF(current_setting('app.user_id', true), '') IS NOT NULL)
    WITH CHECK ("Id" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
        AND NULLIF(current_setting('app.user_id', true), '') IS NOT NULL);

ALTER TABLE app."UserProfiles" ENABLE ROW LEVEL SECURITY;
ALTER TABLE app."UserProfiles" FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_user_scope ON app."UserProfiles" FOR ALL TO PUBLIC
    USING ("TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
        AND "UserId" = NULLIF(current_setting('app.user_id', true), ''))
    WITH CHECK ("TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
        AND "UserId" = NULLIF(current_setting('app.user_id', true), ''));

INSERT INTO app."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261009114112_InitialFoundation', '10.0.12');

COMMIT;

START TRANSACTION;
ALTER TABLE app."Tenants" ADD "OwnerUserId" character varying(256) NOT NULL DEFAULT (COALESCE(NULLIF(current_setting('app.user_id', true), ''), 'legacy-unassigned'));

CREATE TABLE app."TenantMemberships" (
    "TenantId" uuid NOT NULL REFERENCES app."Tenants"("Id") ON DELETE CASCADE,
    "UserId" varchar(256) NOT NULL CHECK(length(btrim("UserId"))>0),
    "Role" varchar(32) NOT NULL CHECK("Role" IN ('User','TenantAdmin','SupportAgent','FinancialAdmin')),
    "IsActive" boolean NOT NULL, "IsPersonal" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamptz NOT NULL, PRIMARY KEY("TenantId","UserId"));
CREATE UNIQUE INDEX "UX_PersonalTenant" ON app."TenantMemberships"("UserId") WHERE "IsPersonal";
CREATE INDEX "IX_Membership_User" ON app."TenantMemberships"("UserId","IsActive");
CREATE TABLE app."UserSessions" (
    "Id" uuid PRIMARY KEY, "UserId" varchar(256) NOT NULL,
    "CookieHash" char(64) NOT NULL, "TokenHash" char(64) NOT NULL,
    "AccessExpiresAt" timestamptz NOT NULL, "ExpiresAt" timestamptz NOT NULL,
    "CreatedAt" timestamptz NOT NULL, "RevokedAt" timestamptz);
CREATE INDEX "IX_Sessions_User" ON app."UserSessions"("UserId","ExpiresAt") WHERE "RevokedAt" IS NULL;
CREATE TABLE app."AuditEntries" (
    "Id" uuid PRIMARY KEY, "UserId" varchar(256) NOT NULL,
    "TenantId" uuid, "Action" varchar(64) NOT NULL, "ResourceId" uuid NOT NULL,
    "Outcome" varchar(32) NOT NULL, "CorrelationId" varchar(128) NOT NULL, "CreatedAt" timestamptz NOT NULL);
CREATE INDEX "IX_Audit_Scope" ON app."AuditEntries"("TenantId","UserId","CreatedAt");
ALTER TABLE app."TenantMemberships" ENABLE ROW LEVEL SECURITY;
ALTER TABLE app."TenantMemberships" FORCE ROW LEVEL SECURITY;
ALTER TABLE app."UserSessions" ENABLE ROW LEVEL SECURITY;
ALTER TABLE app."UserSessions" FORCE ROW LEVEL SECURITY;
ALTER TABLE app."AuditEntries" ENABLE ROW LEVEL SECURITY;
ALTER TABLE app."AuditEntries" FORCE ROW LEVEL SECURITY;
CREATE POLICY membership_read ON app."TenantMemberships" FOR SELECT
    USING ("UserId"=NULLIF(current_setting('app.user_id',true),''));
CREATE POLICY membership_insert ON app."TenantMemberships" FOR INSERT WITH CHECK (
    "TenantId"=NULLIF(current_setting('app.tenant_id',true),'')::uuid
    AND EXISTS (SELECT 1 FROM app."Tenants" t WHERE t."Id"="TenantId"
        AND t."OwnerUserId"=NULLIF(current_setting('app.user_id',true),''))
    AND (NOT "IsPersonal" OR "UserId"=NULLIF(current_setting('app.user_id',true),'')));
CREATE POLICY session_user_scope ON app."UserSessions" FOR ALL
    USING ("UserId"=NULLIF(current_setting('app.user_id',true),''))
    WITH CHECK ("UserId"=NULLIF(current_setting('app.user_id',true),''));
CREATE POLICY audit_read ON app."AuditEntries" FOR SELECT USING (
    "UserId"=NULLIF(current_setting('app.user_id',true),'')
    AND ("TenantId" IS NULL OR "TenantId"=NULLIF(current_setting('app.tenant_id',true),'')::uuid));
CREATE POLICY audit_insert ON app."AuditEntries" FOR INSERT WITH CHECK (
    "UserId"=NULLIF(current_setting('app.user_id',true),'')
    AND ("TenantId" IS NULL OR "TenantId"=NULLIF(current_setting('app.tenant_id',true),'')::uuid));
ALTER POLICY tenant_scope ON app."Tenants" USING (
    "Id"=NULLIF(current_setting('app.tenant_id',true),'')::uuid
    AND ("OwnerUserId"=NULLIF(current_setting('app.user_id',true),'')
        OR EXISTS (SELECT 1 FROM app."TenantMemberships" m
            WHERE m."TenantId"="Id" AND m."UserId"=NULLIF(current_setting('app.user_id',true),'') AND m."IsActive")))
    WITH CHECK (
    "Id"=NULLIF(current_setting('app.tenant_id',true),'')::uuid
    AND "OwnerUserId"=NULLIF(current_setting('app.user_id',true),''));

INSERT INTO app."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261009165110_IdentityFoundation', '10.0.12');

COMMIT;

START TRANSACTION;
CREATE TABLE app."Accounts" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" character varying(256) NOT NULL,
    "Name" character varying(120) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "Balance" numeric(16,2) NOT NULL,
    "IsArchived" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_Accounts" PRIMARY KEY ("TenantId", "UserId", "Id"),
    CONSTRAINT "CK_Accounts_Balance" CHECK ("Balance" >= 0),
    CONSTRAINT "CK_Accounts_Currency" CHECK ("Currency" IN ('BRL','USD','EUR')),
    CONSTRAINT "CK_Accounts_Name" CHECK (length(btrim("Name")) BETWEEN 1 AND 120),
    CONSTRAINT "CK_Accounts_User" CHECK (length(btrim("UserId")) BETWEEN 1 AND 256),
    CONSTRAINT "CK_Accounts_Version" CHECK ("Version" > 0),
    CONSTRAINT "FK_Accounts_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES app."Tenants" ("Id") ON DELETE RESTRICT
);

CREATE TABLE app."Categories" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" character varying(256) NOT NULL,
    "Name" character varying(120) NOT NULL,
    "Flow" character varying(7) NOT NULL,
    "IsArchived" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_Categories" PRIMARY KEY ("TenantId", "UserId", "Id"),
    CONSTRAINT "CK_Categories_Flow" CHECK ("Flow" IN ('Income','Expense')),
    CONSTRAINT "CK_Categories_Name" CHECK (length(btrim("Name")) BETWEEN 1 AND 120),
    CONSTRAINT "CK_Categories_User" CHECK (length(btrim("UserId")) BETWEEN 1 AND 256),
    CONSTRAINT "CK_Categories_Version" CHECK ("Version" > 0),
    CONSTRAINT "FK_Categories_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES app."Tenants" ("Id") ON DELETE RESTRICT
);

CREATE TABLE app."CreditCards" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" character varying(256) NOT NULL,
    "Name" character varying(120) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "Limit" numeric(16,2) NOT NULL,
    "Outstanding" numeric(16,2) NOT NULL,
    "ClosingDay" integer NOT NULL,
    "DueDay" integer NOT NULL,
    "IsArchived" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_CreditCards" PRIMARY KEY ("TenantId", "UserId", "Id"),
    CONSTRAINT "CK_CreditCards_Balances" CHECK ("Limit" > 0 AND "Outstanding" BETWEEN 0 AND "Limit"),
    CONSTRAINT "CK_CreditCards_Currency" CHECK ("Currency" IN ('BRL','USD','EUR')),
    CONSTRAINT "CK_CreditCards_Days" CHECK ("ClosingDay" BETWEEN 1 AND 28 AND "DueDay" BETWEEN 1 AND 28),
    CONSTRAINT "CK_CreditCards_Name" CHECK (length(btrim("Name")) BETWEEN 1 AND 120),
    CONSTRAINT "CK_CreditCards_User" CHECK (length(btrim("UserId")) BETWEEN 1 AND 256),
    CONSTRAINT "CK_CreditCards_Version" CHECK ("Version" > 0),
    CONSTRAINT "FK_CreditCards_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES app."Tenants" ("Id") ON DELETE RESTRICT
);

CREATE TABLE app."FinancialOutbox" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" character varying(256) NOT NULL,
    "OperationId" uuid NOT NULL,
    "ResourceId" uuid NOT NULL,
    "EventType" character varying(64) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_FinancialOutbox" PRIMARY KEY ("TenantId", "UserId", "Id"),
    CONSTRAINT "CK_FinancialOutbox_User" CHECK (length(btrim("UserId")) BETWEEN 1 AND 256),
    CONSTRAINT "FK_FinancialOutbox_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES app."Tenants" ("Id") ON DELETE RESTRICT
);

CREATE TABLE app."SyncOperations" (
    "TenantId" uuid NOT NULL,
    "OperationId" uuid NOT NULL,
    "UserId" character varying(256) NOT NULL,
    "PayloadHash" character varying(64) NOT NULL,
    "ResponseJson" jsonb NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_SyncOperations" PRIMARY KEY ("TenantId", "OperationId"),
    CONSTRAINT "CK_SyncOperations_Hash" CHECK (length("PayloadHash")=64),
    CONSTRAINT "CK_SyncOperations_User" CHECK (length(btrim("UserId")) BETWEEN 1 AND 256),
    CONSTRAINT "FK_SyncOperations_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES app."Tenants" ("Id") ON DELETE RESTRICT
);

CREATE TABLE app."Transfers" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" character varying(256) NOT NULL,
    "SourceAccountId" uuid NOT NULL,
    "DestinationAccountId" uuid NOT NULL,
    "Amount" numeric(16,2) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "OccurredAt" timestamp with time zone NOT NULL,
    "Description" character varying(500) NOT NULL,
    "OperationId" uuid NOT NULL,
    "Status" character varying(9) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_Transfers" PRIMARY KEY ("TenantId", "UserId", "Id"),
    CONSTRAINT "CK_Transfers_Accounts" CHECK ("SourceAccountId" <> "DestinationAccountId"),
    CONSTRAINT "CK_Transfers_Amount" CHECK ("Amount" > 0),
    CONSTRAINT "CK_Transfers_Currency" CHECK ("Currency" IN ('BRL','USD','EUR')),
    CONSTRAINT "CK_Transfers_Status" CHECK ("Status" = 'Posted'),
    CONSTRAINT "CK_Transfers_User" CHECK (length(btrim("UserId")) BETWEEN 1 AND 256),
    CONSTRAINT "CK_Transfers_Version" CHECK ("Version" > 0),
    CONSTRAINT "FK_Transfers_Accounts_TenantId_UserId_DestinationAccountId" FOREIGN KEY ("TenantId", "UserId", "DestinationAccountId") REFERENCES app."Accounts" ("TenantId", "UserId", "Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Transfers_Accounts_TenantId_UserId_SourceAccountId" FOREIGN KEY ("TenantId", "UserId", "SourceAccountId") REFERENCES app."Accounts" ("TenantId", "UserId", "Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Transfers_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES app."Tenants" ("Id") ON DELETE RESTRICT
);

CREATE TABLE app."Transactions" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" character varying(256) NOT NULL,
    "AccountId" uuid NOT NULL,
    "CategoryId" uuid NOT NULL,
    "Amount" numeric(16,2) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "Flow" character varying(7) NOT NULL,
    "Status" character varying(9) NOT NULL,
    "OccurredAt" timestamp with time zone NOT NULL,
    "Description" character varying(500) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_Transactions" PRIMARY KEY ("TenantId", "UserId", "Id"),
    CONSTRAINT "CK_Transactions_Amount" CHECK ("Amount" > 0),
    CONSTRAINT "CK_Transactions_Currency" CHECK ("Currency" IN ('BRL','USD','EUR')),
    CONSTRAINT "CK_Transactions_Flow" CHECK ("Flow" IN ('Income','Expense')),
    CONSTRAINT "CK_Transactions_Status" CHECK ("Status" IN ('Pending','Posted','Cancelled')),
    CONSTRAINT "CK_Transactions_User" CHECK (length(btrim("UserId")) BETWEEN 1 AND 256),
    CONSTRAINT "CK_Transactions_Version" CHECK ("Version" > 0),
    CONSTRAINT "FK_Transactions_Accounts_TenantId_UserId_AccountId" FOREIGN KEY ("TenantId", "UserId", "AccountId") REFERENCES app."Accounts" ("TenantId", "UserId", "Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Transactions_Categories_TenantId_UserId_CategoryId" FOREIGN KEY ("TenantId", "UserId", "CategoryId") REFERENCES app."Categories" ("TenantId", "UserId", "Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Transactions_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES app."Tenants" ("Id") ON DELETE RESTRICT
);

CREATE TABLE app."CardMovements" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" character varying(256) NOT NULL,
    "CardId" uuid NOT NULL,
    "AccountId" uuid,
    "CategoryId" uuid,
    "Amount" numeric(16,2) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "Kind" character varying(8) NOT NULL,
    "OccurredAt" timestamp with time zone NOT NULL,
    "Description" character varying(500) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_CardMovements" PRIMARY KEY ("TenantId", "UserId", "Id"),
    CONSTRAINT "CK_CardMovements_Amount" CHECK ("Amount" > 0),
    CONSTRAINT "CK_CardMovements_Currency" CHECK ("Currency" IN ('BRL','USD','EUR')),
    CONSTRAINT "CK_CardMovements_Shape" CHECK (("Kind"='Purchase' AND "CategoryId" IS NOT NULL AND "AccountId" IS NULL) OR ("Kind"='Payment' AND "AccountId" IS NOT NULL AND "CategoryId" IS NULL)),
    CONSTRAINT "CK_CardMovements_User" CHECK (length(btrim("UserId")) BETWEEN 1 AND 256),
    CONSTRAINT "FK_CardMovements_Accounts_TenantId_UserId_AccountId" FOREIGN KEY ("TenantId", "UserId", "AccountId") REFERENCES app."Accounts" ("TenantId", "UserId", "Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CardMovements_Categories_TenantId_UserId_CategoryId" FOREIGN KEY ("TenantId", "UserId", "CategoryId") REFERENCES app."Categories" ("TenantId", "UserId", "Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CardMovements_CreditCards_TenantId_UserId_CardId" FOREIGN KEY ("TenantId", "UserId", "CardId") REFERENCES app."CreditCards" ("TenantId", "UserId", "Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CardMovements_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES app."Tenants" ("Id") ON DELETE RESTRICT
);

CREATE TABLE app."LedgerEntries" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" character varying(256) NOT NULL,
    "AccountId" uuid NOT NULL,
    "Delta" numeric(16,2) NOT NULL,
    "Kind" character varying(16) NOT NULL,
    "ResourceId" uuid NOT NULL,
    "TransferId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_LedgerEntries" PRIMARY KEY ("TenantId", "UserId", "Id"),
    CONSTRAINT "CK_LedgerEntries_Delta" CHECK ("Delta" <> 0),
    CONSTRAINT "CK_LedgerEntries_Kind" CHECK ("Kind" IN ('Opening','Transaction','Adjustment','Reversal','Transfer','CardPayment')),
    CONSTRAINT "CK_LedgerEntries_Transfer" CHECK (("Kind" = 'Transfer') = ("TransferId" IS NOT NULL) AND ("TransferId" IS NULL OR "ResourceId" = "TransferId")),
    CONSTRAINT "CK_LedgerEntries_User" CHECK (length(btrim("UserId")) BETWEEN 1 AND 256),
    CONSTRAINT "FK_LedgerEntries_Accounts_TenantId_UserId_AccountId" FOREIGN KEY ("TenantId", "UserId", "AccountId") REFERENCES app."Accounts" ("TenantId", "UserId", "Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_LedgerEntries_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES app."Tenants" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_LedgerEntries_Transfers_TenantId_UserId_TransferId" FOREIGN KEY ("TenantId", "UserId", "TransferId") REFERENCES app."Transfers" ("TenantId", "UserId", "Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_CardMovements_TenantId_UserId_AccountId" ON app."CardMovements" ("TenantId", "UserId", "AccountId");

CREATE INDEX "IX_CardMovements_TenantId_UserId_CardId_OccurredAt_Id" ON app."CardMovements" ("TenantId", "UserId", "CardId", "OccurredAt", "Id");

CREATE INDEX "IX_CardMovements_TenantId_UserId_CategoryId" ON app."CardMovements" ("TenantId", "UserId", "CategoryId");

CREATE INDEX "IX_FinancialOutbox_TenantId_UserId_CreatedAt_Id" ON app."FinancialOutbox" ("TenantId", "UserId", "CreatedAt", "Id");

CREATE INDEX "IX_LedgerEntries_TenantId_UserId_AccountId" ON app."LedgerEntries" ("TenantId", "UserId", "AccountId");

CREATE INDEX "IX_LedgerEntries_TenantId_UserId_TransferId" ON app."LedgerEntries" ("TenantId", "UserId", "TransferId");

CREATE INDEX "IX_Transactions_TenantId_UserId_AccountId" ON app."Transactions" ("TenantId", "UserId", "AccountId");

CREATE INDEX "IX_Transactions_TenantId_UserId_CategoryId" ON app."Transactions" ("TenantId", "UserId", "CategoryId");

CREATE INDEX "IX_Transactions_TenantId_UserId_OccurredAt_Id" ON app."Transactions" ("TenantId", "UserId", "OccurredAt", "Id");

CREATE UNIQUE INDEX "IX_Transfers_TenantId_OperationId" ON app."Transfers" ("TenantId", "OperationId");

CREATE INDEX "IX_Transfers_TenantId_UserId_DestinationAccountId" ON app."Transfers" ("TenantId", "UserId", "DestinationAccountId");

CREATE INDEX "IX_Transfers_TenantId_UserId_OccurredAt_Id" ON app."Transfers" ("TenantId", "UserId", "OccurredAt", "Id");

CREATE INDEX "IX_Transfers_TenantId_UserId_SourceAccountId" ON app."Transfers" ("TenantId", "UserId", "SourceAccountId");

DO $rls$
DECLARE target text;
BEGIN
  FOREACH target IN ARRAY ARRAY['Accounts','Categories','CreditCards','Transactions','Transfers',
    'LedgerEntries','CardMovements','SyncOperations','FinancialOutbox']
  LOOP
    EXECUTE format('ALTER TABLE app.%I ENABLE ROW LEVEL SECURITY', target);
    EXECUTE format('ALTER TABLE app.%I FORCE ROW LEVEL SECURITY', target);
    EXECUTE format('CREATE POLICY finance_user_scope ON app.%I FOR ALL USING (
      "TenantId" = NULLIF(current_setting(''app.tenant_id'', true), '''')::uuid
      AND "UserId" = NULLIF(current_setting(''app.user_id'', true), '''')
    ) WITH CHECK (
      "TenantId" = NULLIF(current_setting(''app.tenant_id'', true), '''')::uuid
      AND "UserId" = NULLIF(current_setting(''app.user_id'', true), '''')
    )', target);
  END LOOP;
END $rls$;

CREATE OR REPLACE FUNCTION app.assert_transfer_ledger()
RETURNS trigger LANGUAGE plpgsql SECURITY INVOKER SET search_path = app, pg_temp AS $$
DECLARE
  expected_amount numeric(16,2);
  expected_source uuid;
  expected_destination uuid;
  ledger_count integer;
  ledger_total numeric(16,2);
  source_delta numeric(16,2);
  destination_delta numeric(16,2);
BEGIN
  SELECT "Amount", "SourceAccountId", "DestinationAccountId"
    INTO expected_amount, expected_source, expected_destination
  FROM app."Transfers"
  WHERE "TenantId" = NEW."TenantId" AND "UserId" = NEW."UserId" AND "Id" = NEW."TransferId";
  SELECT count(*), coalesce(sum("Delta"), 0),
    coalesce(sum("Delta") FILTER (WHERE "AccountId" = expected_source), 0),
    coalesce(sum("Delta") FILTER (WHERE "AccountId" = expected_destination), 0)
    INTO ledger_count, ledger_total, source_delta, destination_delta
  FROM app."LedgerEntries"
  WHERE "TenantId" = NEW."TenantId" AND "UserId" = NEW."UserId"
    AND "TransferId" = NEW."TransferId";
  IF ledger_count <> 2 OR ledger_total <> 0
    OR source_delta <> -expected_amount OR destination_delta <> expected_amount THEN
    RAISE EXCEPTION 'Transfer ledger must be a balanced debit and credit' USING ERRCODE = '23514';
  END IF;
  RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER ck_transfer_ledger_balanced
  AFTER INSERT OR UPDATE ON app."LedgerEntries"
  DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
  WHEN (NEW."TransferId" IS NOT NULL)
  EXECUTE FUNCTION app.assert_transfer_ledger();

GRANT SELECT, INSERT, UPDATE ON app."Accounts", app."Categories", app."CreditCards",
  app."Transactions" TO rass_app_runtime;
GRANT SELECT, INSERT ON app."Transfers", app."LedgerEntries", app."CardMovements",
  app."SyncOperations", app."FinancialOutbox" TO rass_app_runtime;
REVOKE ALL ON FUNCTION app.assert_transfer_ledger() FROM PUBLIC;
GRANT EXECUTE ON FUNCTION app.assert_transfer_ledger() TO rass_app_runtime;

INSERT INTO app."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261011222639_CoreFinance', '10.0.12');

COMMIT;

START TRANSACTION;
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

INSERT INTO app."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261011223235_CoreFinanceHardening', '10.0.12');

COMMIT;

START TRANSACTION;
CREATE TABLE app."Budgets" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" character varying(256) NOT NULL,
    "CategoryId" uuid,
    "Amount" numeric(16,2) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "PeriodStart" timestamp with time zone NOT NULL,
    "PeriodEnd" timestamp with time zone NOT NULL,
    "IsArchived" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_Budgets" PRIMARY KEY ("TenantId", "UserId", "Id"),
    CONSTRAINT "CK_Budgets_Amount" CHECK ("Amount" > 0),
    CONSTRAINT "CK_Budgets_Currency" CHECK ("Currency" IN ('BRL','USD','EUR')),
    CONSTRAINT "CK_Budgets_Period" CHECK ("PeriodStart" < "PeriodEnd"),
    CONSTRAINT "CK_Budgets_User" CHECK (length(btrim("UserId")) BETWEEN 1 AND 256),
    CONSTRAINT "CK_Budgets_Version" CHECK ("Version" > 0),
    CONSTRAINT "FK_Budgets_Categories_TenantId_UserId_CategoryId" FOREIGN KEY ("TenantId", "UserId", "CategoryId") REFERENCES app."Categories" ("TenantId", "UserId", "Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Budgets_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES app."Tenants" ("Id") ON DELETE RESTRICT
);

CREATE TABLE app."Goals" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" character varying(256) NOT NULL,
    "Name" character varying(120) NOT NULL,
    "TargetAmount" numeric(16,2) NOT NULL,
    "CurrentAmount" numeric(16,2) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "TargetDate" timestamp with time zone,
    "Status" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_Goals" PRIMARY KEY ("TenantId", "UserId", "Id"),
    CONSTRAINT "CK_Goals_Amounts" CHECK ("TargetAmount" > 0 AND "CurrentAmount" BETWEEN 0 AND "TargetAmount"),
    CONSTRAINT "CK_Goals_Currency" CHECK ("Currency" IN ('BRL','USD','EUR')),
    CONSTRAINT "CK_Goals_Name" CHECK (length(btrim("Name")) BETWEEN 1 AND 120),
    CONSTRAINT "CK_Goals_Status" CHECK ("Status" IN ('Active','Completed','Archived')),
    CONSTRAINT "CK_Goals_User" CHECK (length(btrim("UserId")) BETWEEN 1 AND 256),
    CONSTRAINT "CK_Goals_Version" CHECK ("Version" > 0),
    CONSTRAINT "FK_Goals_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES app."Tenants" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_Budgets_TenantId_UserId_CategoryId" ON app."Budgets" ("TenantId", "UserId", "CategoryId");

CREATE INDEX "IX_Budgets_TenantId_UserId_PeriodStart_PeriodEnd" ON app."Budgets" ("TenantId", "UserId", "PeriodStart", "PeriodEnd");

CREATE INDEX "IX_Goals_TenantId_UserId_Status" ON app."Goals" ("TenantId", "UserId", "Status");

DO $rls$
DECLARE target text;
BEGIN
  FOREACH target IN ARRAY ARRAY['Budgets','Goals']
  LOOP
    EXECUTE format('ALTER TABLE app.%I ENABLE ROW LEVEL SECURITY', target);
    EXECUTE format('ALTER TABLE app.%I FORCE ROW LEVEL SECURITY', target);
    EXECUTE format('CREATE POLICY finance_user_scope ON app.%I FOR ALL USING (
      "TenantId" = NULLIF(current_setting(''app.tenant_id'', true), '''')::uuid
      AND "UserId" = NULLIF(current_setting(''app.user_id'', true), '''')
    ) WITH CHECK (
      "TenantId" = NULLIF(current_setting(''app.tenant_id'', true), '''')::uuid
      AND "UserId" = NULLIF(current_setting(''app.user_id'', true), '''')
    )', target);
  END LOOP;
END $rls$;
GRANT SELECT, INSERT, UPDATE ON app."Budgets", app."Goals" TO rass_app_runtime;

INSERT INTO app."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261012033138_PlanningAndDashboard', '10.0.12');

COMMIT;

