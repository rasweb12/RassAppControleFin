START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
    ALTER TABLE app."CreditCards" DROP CONSTRAINT "CK_CreditCards_Days";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
    ALTER TABLE app."CreditCards" ADD CONSTRAINT "CK_CreditCards_Days" CHECK ("ClosingDay" BETWEEN 1 AND 31 AND "DueDay" BETWEEN 1 AND 31);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
    ALTER TABLE app."CreditCards" ADD "AccentColor" character varying(16);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
    ALTER TABLE app."CreditCards" ADD "Brand" character varying(40);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
    ALTER TABLE app."CreditCards" ADD "Institution" character varying(120) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
    ALTER TABLE app."CreditCards" ADD "PreferredPaymentAccountId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
    CREATE INDEX "IX_CreditCards_TenantId_UserId_PreferredPaymentAccountId" ON app."CreditCards" ("TenantId", "UserId", "PreferredPaymentAccountId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
    CREATE INDEX "IX_CreditCardInstallments_TenantId_UserId_CardId" ON app."CreditCardInstallments" ("TenantId", "UserId", "CardId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
    CREATE INDEX "IX_CreditCardInstallments_TenantId_UserId_InvoiceId_Competence" ON app."CreditCardInstallments" ("TenantId", "UserId", "InvoiceId", "Competence");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
    CREATE UNIQUE INDEX "IX_CreditCardInstallments_TenantId_UserId_PurchaseId_Number" ON app."CreditCardInstallments" ("TenantId", "UserId", "PurchaseId", "Number");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
    CREATE UNIQUE INDEX "IX_CreditCardInvoices_TenantId_UserId_CardId_Competence" ON app."CreditCardInvoices" ("TenantId", "UserId", "CardId", "Competence");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
    CREATE INDEX "IX_CreditCardPurchases_TenantId_UserId_CardId_PurchasedOn_Id" ON app."CreditCardPurchases" ("TenantId", "UserId", "CardId", "PurchasedOn", "Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
    CREATE INDEX "IX_CreditCardPurchases_TenantId_UserId_CategoryId" ON app."CreditCardPurchases" ("TenantId", "UserId", "CategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
    ALTER TABLE app."CreditCards" ADD CONSTRAINT "FK_CreditCards_Accounts_TenantId_UserId_PreferredPaymentAccoun~" FOREIGN KEY ("TenantId", "UserId", "PreferredPaymentAccountId") REFERENCES app."Accounts" ("TenantId", "UserId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101013424_CreditCardBillingCore') THEN
    INSERT INTO app."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261101013424_CreditCardBillingCore', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101030821_CreditCardBillingMovements') THEN
    ALTER TABLE app."CardMovements" ADD "InvoiceId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101030821_CreditCardBillingMovements') THEN
    ALTER TABLE app."CardMovements" ADD "PurchaseId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101030821_CreditCardBillingMovements') THEN
    CREATE INDEX "IX_CardMovements_TenantId_UserId_InvoiceId" ON app."CardMovements" ("TenantId", "UserId", "InvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101030821_CreditCardBillingMovements') THEN
    ALTER TABLE app."CardMovements" ADD CONSTRAINT "FK_CardMovements_CreditCardInvoices_TenantId_UserId_InvoiceId" FOREIGN KEY ("TenantId", "UserId", "InvoiceId") REFERENCES app."CreditCardInvoices" ("TenantId", "UserId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101030821_CreditCardBillingMovements') THEN
    ALTER TABLE app."CardMovements" ADD CONSTRAINT "FK_CardMovements_CreditCardPurchases_TenantId_UserId_PurchaseId" FOREIGN KEY ("TenantId", "UserId", "PurchaseId") REFERENCES app."CreditCardPurchases" ("TenantId", "UserId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101030821_CreditCardBillingMovements') THEN
    INSERT INTO app."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261101030821_CreditCardBillingMovements', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101031055_CreditCardBillingIndexes') THEN
    CREATE INDEX "IX_CardMovements_TenantId_UserId_PurchaseId" ON app."CardMovements" ("TenantId", "UserId", "PurchaseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101031055_CreditCardBillingIndexes') THEN
    INSERT INTO app."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261101031055_CreditCardBillingIndexes', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101040238_CreditCardModelAlignment') THEN
    INSERT INTO app."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261101040238_CreditCardModelAlignment', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101052030_PlatformRbacFoundation') THEN
    CREATE TABLE app."PlatformRoleAssignments" (
        "Id" uuid NOT NULL,
        "UserId" character varying(256) NOT NULL,
        "Role" character varying(32) NOT NULL,
        "IsActive" boolean NOT NULL,
        "GrantedBy" character varying(256) NOT NULL,
        "GrantedAt" timestamp with time zone NOT NULL,
        "RevokedBy" character varying(256),
        "RevokedAt" timestamp with time zone,
        "Reason" character varying(500) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PlatformRoleAssignments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101052030_PlatformRbacFoundation') THEN
    CREATE INDEX "IX_PlatformRoleAssignments_Role_IsActive" ON app."PlatformRoleAssignments" ("Role", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101052030_PlatformRbacFoundation') THEN
    CREATE UNIQUE INDEX "IX_PlatformRoleAssignments_UserId_Role" ON app."PlatformRoleAssignments" ("UserId", "Role") WHERE "IsActive";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101052030_PlatformRbacFoundation') THEN
    ALTER TABLE app."PlatformRoleAssignments"
        ADD CONSTRAINT "CK_PlatformRoleAssignments_Role" CHECK ("Role" IN ('SuperAdmin'));
    ALTER TABLE app."PlatformRoleAssignments" ENABLE ROW LEVEL SECURITY;
    ALTER TABLE app."PlatformRoleAssignments" FORCE ROW LEVEL SECURITY;
    CREATE POLICY platform_assignment_self_read ON app."PlatformRoleAssignments" FOR SELECT
        USING ("UserId" = NULLIF(current_setting('app.user_id', true), '')
            OR current_setting('app.platform_authorized', true) = 'true');
    CREATE POLICY platform_assignment_admin_insert ON app."PlatformRoleAssignments" FOR INSERT
        WITH CHECK (current_setting('app.platform_authorized', true) = 'true'
            AND "GrantedBy" = NULLIF(current_setting('app.user_id', true), ''));
    CREATE POLICY platform_assignment_admin_update ON app."PlatformRoleAssignments" FOR UPDATE
        USING (current_setting('app.platform_authorized', true) = 'true')
        WITH CHECK (current_setting('app.platform_authorized', true) = 'true');
    GRANT SELECT, INSERT, UPDATE ON app."PlatformRoleAssignments" TO rass_app_runtime_v2;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101052030_PlatformRbacFoundation') THEN
    INSERT INTO app."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261101052030_PlatformRbacFoundation', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101100000_PlatformAdministrationIsolation') THEN
    DO $$ BEGIN
      IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_platform_admin_v1')
         OR NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_platform_executor_owner_v1') THEN
        RAISE EXCEPTION 'Platform administration roles must be provisioned before PlatformAdministrationIsolation';
      END IF;
    END $$;

    REVOKE INSERT, UPDATE ON app."PlatformRoleAssignments" FROM rass_app_runtime_v2;
    GRANT SELECT ON app."PlatformRoleAssignments" TO rass_app_runtime_v2;

    DROP POLICY IF EXISTS platform_assignment_self_read ON app."PlatformRoleAssignments";
    DROP POLICY IF EXISTS platform_assignment_admin_insert ON app."PlatformRoleAssignments";
    DROP POLICY IF EXISTS platform_assignment_admin_update ON app."PlatformRoleAssignments";

    CREATE POLICY platform_assignment_runtime_self_read ON app."PlatformRoleAssignments"
      FOR SELECT TO rass_app_runtime_v2
      USING ("UserId" = NULLIF(current_setting('app.user_id', true), ''));
    CREATE POLICY platform_assignment_executor_read ON app."PlatformRoleAssignments"
      FOR SELECT TO rass_platform_executor_owner_v1 USING (true);
    CREATE POLICY platform_assignment_executor_insert ON app."PlatformRoleAssignments"
      FOR INSERT TO rass_platform_executor_owner_v1 WITH CHECK (true);
    CREATE POLICY platform_assignment_executor_update ON app."PlatformRoleAssignments"
      FOR UPDATE TO rass_platform_executor_owner_v1 USING (true) WITH CHECK (true);
    CREATE POLICY platform_executor_audit_insert ON app."AuditEntries"
      FOR INSERT TO rass_platform_executor_owner_v1 WITH CHECK (true);
    CREATE POLICY platform_executor_tenant_read ON app."Tenants"
      FOR SELECT TO rass_platform_executor_owner_v1 USING (true);
    CREATE POLICY platform_executor_profile_read ON app."UserProfiles"
      FOR SELECT TO rass_platform_executor_owner_v1 USING (true);

    GRANT USAGE ON SCHEMA app TO rass_platform_executor_owner_v1, rass_platform_admin_v1;
    GRANT SELECT, INSERT, UPDATE ON app."PlatformRoleAssignments", app."AuditEntries" TO rass_platform_executor_owner_v1;
    GRANT SELECT ON app."Tenants", app."UserProfiles" TO rass_platform_executor_owner_v1;

    CREATE FUNCTION app.platform_dashboard()
    RETURNS TABLE(users bigint, tenants bigint, super_admins bigint)
    LANGUAGE sql SECURITY DEFINER SET search_path = pg_catalog, app AS $$
      SELECT (SELECT count(DISTINCT "UserId") FROM app."UserProfiles"),
             (SELECT count(*) FROM app."Tenants"),
             (SELECT count(*) FROM app."PlatformRoleAssignments" WHERE "IsActive" AND "Role"='SuperAdmin');
    $$;
    CREATE FUNCTION app.platform_roles(p_user_id text)
    RETURNS TABLE(id uuid, user_id text, role text, is_active boolean, granted_by text, granted_at timestamptz, revoked_by text, revoked_at timestamptz, reason text)
    LANGUAGE sql SECURITY DEFINER SET search_path = pg_catalog, app AS $$
      SELECT "Id","UserId","Role","IsActive","GrantedBy","GrantedAt","RevokedBy","RevokedAt","Reason"
      FROM app."PlatformRoleAssignments" WHERE "UserId"=p_user_id ORDER BY "CreatedAt" DESC;
    $$;
    CREATE FUNCTION app.platform_grant_superadmin(p_assignment_id uuid, p_audit_id uuid, p_actor text, p_target text, p_reason text, p_correlation text)
    RETURNS TABLE(id uuid, granted_at timestamptz)
    LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog, app AS $$
    DECLARE v_now timestamptz := clock_timestamp();
    BEGIN
      IF p_actor IS NULL OR p_target IS NULL OR p_actor='' OR p_target='' OR p_actor=p_target OR length(p_reason) NOT BETWEEN 1 AND 500 THEN
        RAISE EXCEPTION 'platform role request rejected' USING ERRCODE='P0001';
      END IF;
      PERFORM pg_advisory_xact_lock(hashtext('rass.platform.superadmin'));
      INSERT INTO app."PlatformRoleAssignments" ("Id","UserId","Role","IsActive","GrantedBy","GrantedAt","Reason","CreatedAt","UpdatedAt")
      VALUES (p_assignment_id,p_target,'SuperAdmin',true,p_actor,v_now,btrim(p_reason),v_now,v_now)
      ON CONFLICT ("UserId","Role") WHERE "IsActive" DO NOTHING
      RETURNING "Id","GrantedAt" INTO id,granted_at;
      IF id IS NULL THEN RAISE EXCEPTION 'platform role conflict' USING ERRCODE='P0001'; END IF;
      INSERT INTO app."AuditEntries" ("Id","UserId","TenantId","Action","ResourceId","Outcome","CorrelationId","CreatedAt")
      VALUES (p_audit_id,p_actor,NULL,'platform.role.granted',id,'success',p_correlation,v_now);
      RETURN NEXT;
    END;
    $$;
    CREATE FUNCTION app.platform_revoke_superadmin(p_audit_id uuid, p_actor text, p_target text, p_assignment_id uuid, p_reason text, p_correlation text)
    RETURNS boolean
    LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog, app AS $$
    DECLARE v_user text; v_active boolean; v_count bigint; v_now timestamptz := clock_timestamp();
    BEGIN
      IF p_actor IS NULL OR p_target IS NULL OR p_actor='' OR p_target='' OR p_actor=p_target OR length(p_reason) NOT BETWEEN 1 AND 500 THEN RETURN false; END IF;
      PERFORM pg_advisory_xact_lock(hashtext('rass.platform.superadmin'));
      SELECT "UserId","IsActive",(SELECT count(*) FROM app."PlatformRoleAssignments" WHERE "IsActive" AND "Role"='SuperAdmin')
        INTO v_user,v_active,v_count FROM app."PlatformRoleAssignments" WHERE "Id"=p_assignment_id FOR UPDATE;
      IF NOT FOUND OR v_user<>p_target OR NOT v_active OR v_count<=1 THEN RETURN false; END IF;
      UPDATE app."PlatformRoleAssignments" SET "IsActive"=false,"RevokedBy"=p_actor,"RevokedAt"=v_now,"Reason"=btrim(p_reason),"UpdatedAt"=v_now
        WHERE "Id"=p_assignment_id AND "UserId"=p_target AND "IsActive";
      INSERT INTO app."AuditEntries" ("Id","UserId","TenantId","Action","ResourceId","Outcome","CorrelationId","CreatedAt")
      VALUES (p_audit_id,p_actor,NULL,'platform.role.revoked',p_assignment_id,'success',p_correlation,v_now);
      RETURN true;
    END;
    $$;
    ALTER FUNCTION app.platform_dashboard() OWNER TO rass_platform_executor_owner_v1;
    ALTER FUNCTION app.platform_roles(text) OWNER TO rass_platform_executor_owner_v1;
    ALTER FUNCTION app.platform_grant_superadmin(uuid,uuid,text,text,text,text) OWNER TO rass_platform_executor_owner_v1;
    ALTER FUNCTION app.platform_revoke_superadmin(uuid,text,text,uuid,text,text) OWNER TO rass_platform_executor_owner_v1;
    REVOKE ALL ON FUNCTION app.platform_dashboard(), app.platform_roles(text), app.platform_grant_superadmin(uuid,uuid,text,text,text,text), app.platform_revoke_superadmin(uuid,text,text,uuid,text,text) FROM PUBLIC;
    GRANT EXECUTE ON FUNCTION app.platform_dashboard(), app.platform_roles(text), app.platform_grant_superadmin(uuid,uuid,text,text,text,text), app.platform_revoke_superadmin(uuid,text,text,uuid,text,text) TO rass_platform_admin_v1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261101100000_PlatformAdministrationIsolation') THEN
    INSERT INTO app."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261101100000_PlatformAdministrationIsolation', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261102010000_PlatformAdministrationHardening') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20261102010000_PlatformAdministrationHardening') THEN
    INSERT INTO app."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261102010000_PlatformAdministrationHardening', '10.0.13');
    END IF;
END $EF$;
COMMIT;

