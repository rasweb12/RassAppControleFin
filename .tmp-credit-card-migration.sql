START TRANSACTION;
ALTER TABLE app."CreditCards" DROP CONSTRAINT "CK_CreditCards_Days";

ALTER TABLE app."CreditCards" ADD CONSTRAINT "CK_CreditCards_Days" CHECK ("ClosingDay" BETWEEN 1 AND 31 AND "DueDay" BETWEEN 1 AND 31);

ALTER TABLE app."CreditCards" ADD "AccentColor" character varying(16);

ALTER TABLE app."CreditCards" ADD "Brand" character varying(40);

ALTER TABLE app."CreditCards" ADD "Institution" character varying(120) NOT NULL DEFAULT '';

ALTER TABLE app."CreditCards" ADD "PreferredPaymentAccountId" uuid;

CREATE TABLE app."CreditCardInvoices" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" character varying(256) NOT NULL,
    "CardId" uuid NOT NULL,
    "Competence" date NOT NULL,
    "ClosingDate" date NOT NULL,
    "DueDate" date NOT NULL,
    "TotalAmount" numeric(16,2) NOT NULL,
    "PaidAmount" numeric(16,2) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "Status" character varying(16) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_CreditCardInvoices" PRIMARY KEY ("TenantId", "UserId", "Id"),
    CONSTRAINT "CK_CreditCardInvoices_Amounts" CHECK ("TotalAmount" >= 0 AND "PaidAmount" BETWEEN 0 AND "TotalAmount"),
    CONSTRAINT "CK_CreditCardInvoices_Currency" CHECK ("Currency" IN ('BRL','USD','EUR')),
    CONSTRAINT "CK_CreditCardInvoices_Dates" CHECK ("ClosingDate" >= "Competence" AND "DueDate" >= "ClosingDate"),
    CONSTRAINT "CK_CreditCardInvoices_Status" CHECK ("Status" IN ('Open','Closed','PartiallyPaid','Paid','Overdue','Cancelled')),
    CONSTRAINT "CK_CreditCardInvoices_User" CHECK (length(btrim("UserId")) BETWEEN 1 AND 256),
    CONSTRAINT "CK_CreditCardInvoices_Version" CHECK ("Version" > 0),
    CONSTRAINT "FK_CreditCardInvoices_CreditCards_TenantId_UserId_CardId" FOREIGN KEY ("TenantId", "UserId", "CardId") REFERENCES app."CreditCards" ("TenantId", "UserId", "Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CreditCardInvoices_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES app."Tenants" ("Id") ON DELETE RESTRICT
);

CREATE TABLE app."CreditCardPurchases" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" character varying(256) NOT NULL,
    "CardId" uuid NOT NULL,
    "CategoryId" uuid NOT NULL,
    "Description" character varying(500) NOT NULL,
    "Amount" numeric(16,2) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "PurchasedOn" date NOT NULL,
    "InstallmentCount" integer NOT NULL,
    "IsCancelled" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_CreditCardPurchases" PRIMARY KEY ("TenantId", "UserId", "Id"),
    CONSTRAINT "CK_CreditCardPurchases_Amount" CHECK ("Amount" > 0),
    CONSTRAINT "CK_CreditCardPurchases_Currency" CHECK ("Currency" IN ('BRL','USD','EUR')),
    CONSTRAINT "CK_CreditCardPurchases_Installments" CHECK ("InstallmentCount" BETWEEN 1 AND 360),
    CONSTRAINT "CK_CreditCardPurchases_User" CHECK (length(btrim("UserId")) BETWEEN 1 AND 256),
    CONSTRAINT "CK_CreditCardPurchases_Version" CHECK ("Version" > 0),
    CONSTRAINT "FK_CreditCardPurchases_Categories_TenantId_UserId_CategoryId" FOREIGN KEY ("TenantId", "UserId", "CategoryId") REFERENCES app."Categories" ("TenantId", "UserId", "Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CreditCardPurchases_CreditCards_TenantId_UserId_CardId" FOREIGN KEY ("TenantId", "UserId", "CardId") REFERENCES app."CreditCards" ("TenantId", "UserId", "Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CreditCardPurchases_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES app."Tenants" ("Id") ON DELETE RESTRICT
);

CREATE TABLE app."CreditCardInstallments" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" character varying(256) NOT NULL,
    "PurchaseId" uuid NOT NULL,
    "CardId" uuid NOT NULL,
    "InvoiceId" uuid NOT NULL,
    "Number" integer NOT NULL,
    "Amount" numeric(16,2) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "Competence" date NOT NULL,
    "Status" character varying(16) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_CreditCardInstallments" PRIMARY KEY ("TenantId", "UserId", "Id"),
    CONSTRAINT "CK_CreditCardInstallments_Amount" CHECK ("Amount" > 0),
    CONSTRAINT "CK_CreditCardInstallments_Currency" CHECK ("Currency" IN ('BRL','USD','EUR')),
    CONSTRAINT "CK_CreditCardInstallments_Number" CHECK ("Number" >= 1),
    CONSTRAINT "CK_CreditCardInstallments_Status" CHECK ("Status" IN ('Scheduled','Posted','Cancelled')),
    CONSTRAINT "CK_CreditCardInstallments_User" CHECK (length(btrim("UserId")) BETWEEN 1 AND 256),
    CONSTRAINT "CK_CreditCardInstallments_Version" CHECK ("Version" > 0),
    CONSTRAINT "FK_CreditCardInstallments_CreditCardInvoices_TenantId_UserId_I~" FOREIGN KEY ("TenantId", "UserId", "InvoiceId") REFERENCES app."CreditCardInvoices" ("TenantId", "UserId", "Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CreditCardInstallments_CreditCardPurchases_TenantId_UserId_~" FOREIGN KEY ("TenantId", "UserId", "PurchaseId") REFERENCES app."CreditCardPurchases" ("TenantId", "UserId", "Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CreditCardInstallments_CreditCards_TenantId_UserId_CardId" FOREIGN KEY ("TenantId", "UserId", "CardId") REFERENCES app."CreditCards" ("TenantId", "UserId", "Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CreditCardInstallments_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES app."Tenants" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_CreditCards_TenantId_UserId_PreferredPaymentAccountId" ON app."CreditCards" ("TenantId", "UserId", "PreferredPaymentAccountId");

CREATE INDEX "IX_CreditCardInstallments_TenantId_UserId_CardId" ON app."CreditCardInstallments" ("TenantId", "UserId", "CardId");

CREATE INDEX "IX_CreditCardInstallments_TenantId_UserId_InvoiceId_Competence" ON app."CreditCardInstallments" ("TenantId", "UserId", "InvoiceId", "Competence");

CREATE UNIQUE INDEX "IX_CreditCardInstallments_TenantId_UserId_PurchaseId_Number" ON app."CreditCardInstallments" ("TenantId", "UserId", "PurchaseId", "Number");

CREATE UNIQUE INDEX "IX_CreditCardInvoices_TenantId_UserId_CardId_Competence" ON app."CreditCardInvoices" ("TenantId", "UserId", "CardId", "Competence");

CREATE INDEX "IX_CreditCardPurchases_TenantId_UserId_CardId_PurchasedOn_Id" ON app."CreditCardPurchases" ("TenantId", "UserId", "CardId", "PurchasedOn", "Id");

CREATE INDEX "IX_CreditCardPurchases_TenantId_UserId_CategoryId" ON app."CreditCardPurchases" ("TenantId", "UserId", "CategoryId");

ALTER TABLE app."CreditCards" ADD CONSTRAINT "FK_CreditCards_Accounts_TenantId_UserId_PreferredPaymentAccoun~" FOREIGN KEY ("TenantId", "UserId", "PreferredPaymentAccountId") REFERENCES app."Accounts" ("TenantId", "UserId", "Id") ON DELETE RESTRICT;

DO $rls$
DECLARE target text;
BEGIN
  FOREACH target IN ARRAY ARRAY['CreditCardPurchases','CreditCardInvoices','CreditCardInstallments']
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
GRANT SELECT, INSERT, UPDATE ON app."CreditCardPurchases", app."CreditCardInvoices",
  app."CreditCardInstallments" TO rass_app_runtime_v2;

INSERT INTO app."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261101013424_CreditCardBillingCore', '10.0.13');

COMMIT;

START TRANSACTION;
ALTER TABLE app."CardMovements" ADD "InvoiceId" uuid;

ALTER TABLE app."CardMovements" ADD "PurchaseId" uuid;

CREATE INDEX "IX_CardMovements_TenantId_UserId_InvoiceId" ON app."CardMovements" ("TenantId", "UserId", "InvoiceId");

ALTER TABLE app."CardMovements" ADD CONSTRAINT "FK_CardMovements_CreditCardInvoices_TenantId_UserId_InvoiceId" FOREIGN KEY ("TenantId", "UserId", "InvoiceId") REFERENCES app."CreditCardInvoices" ("TenantId", "UserId", "Id") ON DELETE RESTRICT;

ALTER TABLE app."CardMovements" ADD CONSTRAINT "FK_CardMovements_CreditCardPurchases_TenantId_UserId_PurchaseId" FOREIGN KEY ("TenantId", "UserId", "PurchaseId") REFERENCES app."CreditCardPurchases" ("TenantId", "UserId", "Id") ON DELETE RESTRICT;

INSERT INTO app."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261101030821_CreditCardBillingMovements', '10.0.13');

COMMIT;

START TRANSACTION;
CREATE INDEX "IX_CardMovements_TenantId_UserId_PurchaseId" ON app."CardMovements" ("TenantId", "UserId", "PurchaseId");

INSERT INTO app."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261101031055_CreditCardBillingIndexes', '10.0.13');

COMMIT;

