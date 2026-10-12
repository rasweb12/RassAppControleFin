using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RassAppControleFin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CoreFinance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_Accounts_Balance", "\"Balance\" >= 0");
                    table.CheckConstraint("CK_Accounts_Currency", "\"Currency\" IN ('BRL','USD','EUR')");
                    table.CheckConstraint("CK_Accounts_Name", "length(btrim(\"Name\")) BETWEEN 1 AND 120");
                    table.CheckConstraint("CK_Accounts_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_Accounts_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Accounts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Flow = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_Categories_Flow", "\"Flow\" IN ('Income','Expense')");
                    table.CheckConstraint("CK_Categories_Name", "length(btrim(\"Name\")) BETWEEN 1 AND 120");
                    table.CheckConstraint("CK_Categories_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_Categories_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Categories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditCards",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Limit = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    Outstanding = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    ClosingDay = table.Column<int>(type: "integer", nullable: false),
                    DueDay = table.Column<int>(type: "integer", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCards", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_CreditCards_Balances", "\"Limit\" > 0 AND \"Outstanding\" BETWEEN 0 AND \"Limit\"");
                    table.CheckConstraint("CK_CreditCards_Currency", "\"Currency\" IN ('BRL','USD','EUR')");
                    table.CheckConstraint("CK_CreditCards_Days", "\"ClosingDay\" BETWEEN 1 AND 28 AND \"DueDay\" BETWEEN 1 AND 28");
                    table.CheckConstraint("CK_CreditCards_Name", "length(btrim(\"Name\")) BETWEEN 1 AND 120");
                    table.CheckConstraint("CK_CreditCards_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_CreditCards_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_CreditCards_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialOutbox",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialOutbox", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_FinancialOutbox_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.ForeignKey(
                        name: "FK_FinancialOutbox_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SyncOperations",
                schema: "app",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResponseJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncOperations", x => new { x.TenantId, x.OperationId });
                    table.CheckConstraint("CK_SyncOperations_Hash", "length(\"PayloadHash\")=64");
                    table.CheckConstraint("CK_SyncOperations_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.ForeignKey(
                        name: "FK_SyncOperations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transfers",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SourceAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transfers", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_Transfers_Accounts", "\"SourceAccountId\" <> \"DestinationAccountId\"");
                    table.CheckConstraint("CK_Transfers_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_Transfers_Currency", "\"Currency\" IN ('BRL','USD','EUR')");
                    table.CheckConstraint("CK_Transfers_Status", "\"Status\" = 'Posted'");
                    table.CheckConstraint("CK_Transfers_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_Transfers_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Transfers_Accounts_TenantId_UserId_DestinationAccountId",
                        columns: x => new { x.TenantId, x.UserId, x.DestinationAccountId },
                        principalSchema: "app",
                        principalTable: "Accounts",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transfers_Accounts_TenantId_UserId_SourceAccountId",
                        columns: x => new { x.TenantId, x.UserId, x.SourceAccountId },
                        principalSchema: "app",
                        principalTable: "Accounts",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transfers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Flow = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Status = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_Transactions_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_Transactions_Currency", "\"Currency\" IN ('BRL','USD','EUR')");
                    table.CheckConstraint("CK_Transactions_Flow", "\"Flow\" IN ('Income','Expense')");
                    table.CheckConstraint("CK_Transactions_Status", "\"Status\" IN ('Pending','Posted','Cancelled')");
                    table.CheckConstraint("CK_Transactions_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_Transactions_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Transactions_Accounts_TenantId_UserId_AccountId",
                        columns: x => new { x.TenantId, x.UserId, x.AccountId },
                        principalSchema: "app",
                        principalTable: "Accounts",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Categories_TenantId_UserId_CategoryId",
                        columns: x => new { x.TenantId, x.UserId, x.CategoryId },
                        principalSchema: "app",
                        principalTable: "Categories",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CardMovements",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CardId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Kind = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardMovements", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_CardMovements_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_CardMovements_Currency", "\"Currency\" IN ('BRL','USD','EUR')");
                    table.CheckConstraint("CK_CardMovements_Shape", "(\"Kind\"='Purchase' AND \"CategoryId\" IS NOT NULL AND \"AccountId\" IS NULL) OR (\"Kind\"='Payment' AND \"AccountId\" IS NOT NULL AND \"CategoryId\" IS NULL)");
                    table.CheckConstraint("CK_CardMovements_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.ForeignKey(
                        name: "FK_CardMovements_Accounts_TenantId_UserId_AccountId",
                        columns: x => new { x.TenantId, x.UserId, x.AccountId },
                        principalSchema: "app",
                        principalTable: "Accounts",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardMovements_Categories_TenantId_UserId_CategoryId",
                        columns: x => new { x.TenantId, x.UserId, x.CategoryId },
                        principalSchema: "app",
                        principalTable: "Categories",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardMovements_CreditCards_TenantId_UserId_CardId",
                        columns: x => new { x.TenantId, x.UserId, x.CardId },
                        principalSchema: "app",
                        principalTable: "CreditCards",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardMovements_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LedgerEntries",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Delta = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerEntries", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_LedgerEntries_Delta", "\"Delta\" <> 0");
                    table.CheckConstraint("CK_LedgerEntries_Kind", "\"Kind\" IN ('Opening','Transaction','Adjustment','Reversal','Transfer','CardPayment')");
                    table.CheckConstraint("CK_LedgerEntries_Transfer", "(\"Kind\" = 'Transfer') = (\"TransferId\" IS NOT NULL) AND (\"TransferId\" IS NULL OR \"ResourceId\" = \"TransferId\")");
                    table.CheckConstraint("CK_LedgerEntries_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.ForeignKey(
                        name: "FK_LedgerEntries_Accounts_TenantId_UserId_AccountId",
                        columns: x => new { x.TenantId, x.UserId, x.AccountId },
                        principalSchema: "app",
                        principalTable: "Accounts",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LedgerEntries_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LedgerEntries_Transfers_TenantId_UserId_TransferId",
                        columns: x => new { x.TenantId, x.UserId, x.TransferId },
                        principalSchema: "app",
                        principalTable: "Transfers",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardMovements_TenantId_UserId_AccountId",
                schema: "app",
                table: "CardMovements",
                columns: new[] { "TenantId", "UserId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_CardMovements_TenantId_UserId_CardId_OccurredAt_Id",
                schema: "app",
                table: "CardMovements",
                columns: new[] { "TenantId", "UserId", "CardId", "OccurredAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CardMovements_TenantId_UserId_CategoryId",
                schema: "app",
                table: "CardMovements",
                columns: new[] { "TenantId", "UserId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialOutbox_TenantId_UserId_CreatedAt_Id",
                schema: "app",
                table: "FinancialOutbox",
                columns: new[] { "TenantId", "UserId", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_TenantId_UserId_AccountId",
                schema: "app",
                table: "LedgerEntries",
                columns: new[] { "TenantId", "UserId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_TenantId_UserId_TransferId",
                schema: "app",
                table: "LedgerEntries",
                columns: new[] { "TenantId", "UserId", "TransferId" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TenantId_UserId_AccountId",
                schema: "app",
                table: "Transactions",
                columns: new[] { "TenantId", "UserId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TenantId_UserId_CategoryId",
                schema: "app",
                table: "Transactions",
                columns: new[] { "TenantId", "UserId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TenantId_UserId_OccurredAt_Id",
                schema: "app",
                table: "Transactions",
                columns: new[] { "TenantId", "UserId", "OccurredAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_TenantId_OperationId",
                schema: "app",
                table: "Transfers",
                columns: new[] { "TenantId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_TenantId_UserId_DestinationAccountId",
                schema: "app",
                table: "Transfers",
                columns: new[] { "TenantId", "UserId", "DestinationAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_TenantId_UserId_OccurredAt_Id",
                schema: "app",
                table: "Transfers",
                columns: new[] { "TenantId", "UserId", "OccurredAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_TenantId_UserId_SourceAccountId",
                schema: "app",
                table: "Transfers",
                columns: new[] { "TenantId", "UserId", "SourceAccountId" });

            migrationBuilder.Sql("""
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                REVOKE EXECUTE ON FUNCTION app.assert_transfer_ledger() FROM rass_app_runtime;
                DROP FUNCTION IF EXISTS app.assert_transfer_ledger();
                """);
            migrationBuilder.DropTable(
                name: "CardMovements",
                schema: "app");

            migrationBuilder.DropTable(
                name: "FinancialOutbox",
                schema: "app");

            migrationBuilder.DropTable(
                name: "LedgerEntries",
                schema: "app");

            migrationBuilder.DropTable(
                name: "SyncOperations",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Transactions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "CreditCards",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Transfers",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Accounts",
                schema: "app");
        }
    }
}
