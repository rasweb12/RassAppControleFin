using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RassAppControleFin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreditCardBillingCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CreditCards_Days",
                schema: "app",
                table: "CreditCards");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CreditCards_Days",
                schema: "app",
                table: "CreditCards",
                sql: "\"ClosingDay\" BETWEEN 1 AND 31 AND \"DueDay\" BETWEEN 1 AND 31");

            migrationBuilder.AddColumn<string>(
                name: "AccentColor",
                schema: "app",
                table: "CreditCards",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                schema: "app",
                table: "CreditCards",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Institution",
                schema: "app",
                table: "CreditCards",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PreferredPaymentAccountId",
                schema: "app",
                table: "CreditCards",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CreditCardInvoices",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Competence = table.Column<DateOnly>(type: "date", nullable: false),
                    ClosingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardInvoices", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_CreditCardInvoices_Amounts", "\"TotalAmount\" >= 0 AND \"PaidAmount\" BETWEEN 0 AND \"TotalAmount\"");
                    table.CheckConstraint("CK_CreditCardInvoices_Currency", "\"Currency\" IN ('BRL','USD','EUR')");
                    table.CheckConstraint("CK_CreditCardInvoices_Dates", "\"ClosingDate\" >= \"Competence\" AND \"DueDate\" >= \"ClosingDate\"");
                    table.CheckConstraint("CK_CreditCardInvoices_Status", "\"Status\" IN ('Open','Closed','PartiallyPaid','Paid','Overdue','Cancelled')");
                    table.CheckConstraint("CK_CreditCardInvoices_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_CreditCardInvoices_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_CreditCardInvoices_CreditCards_TenantId_UserId_CardId",
                        columns: x => new { x.TenantId, x.UserId, x.CardId },
                        principalSchema: "app",
                        principalTable: "CreditCards",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditCardInvoices_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditCardPurchases",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CardId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PurchasedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    InstallmentCount = table.Column<int>(type: "integer", nullable: false),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardPurchases", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_CreditCardPurchases_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_CreditCardPurchases_Currency", "\"Currency\" IN ('BRL','USD','EUR')");
                    table.CheckConstraint("CK_CreditCardPurchases_Installments", "\"InstallmentCount\" BETWEEN 1 AND 360");
                    table.CheckConstraint("CK_CreditCardPurchases_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_CreditCardPurchases_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_CreditCardPurchases_Categories_TenantId_UserId_CategoryId",
                        columns: x => new { x.TenantId, x.UserId, x.CategoryId },
                        principalSchema: "app",
                        principalTable: "Categories",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditCardPurchases_CreditCards_TenantId_UserId_CardId",
                        columns: x => new { x.TenantId, x.UserId, x.CardId },
                        principalSchema: "app",
                        principalTable: "CreditCards",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditCardPurchases_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditCardInstallments",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Competence = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardInstallments", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_CreditCardInstallments_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_CreditCardInstallments_Currency", "\"Currency\" IN ('BRL','USD','EUR')");
                    table.CheckConstraint("CK_CreditCardInstallments_Number", "\"Number\" >= 1");
                    table.CheckConstraint("CK_CreditCardInstallments_Status", "\"Status\" IN ('Scheduled','Posted','Cancelled')");
                    table.CheckConstraint("CK_CreditCardInstallments_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_CreditCardInstallments_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_CreditCardInstallments_CreditCardInvoices_TenantId_UserId_I~",
                        columns: x => new { x.TenantId, x.UserId, x.InvoiceId },
                        principalSchema: "app",
                        principalTable: "CreditCardInvoices",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditCardInstallments_CreditCardPurchases_TenantId_UserId_~",
                        columns: x => new { x.TenantId, x.UserId, x.PurchaseId },
                        principalSchema: "app",
                        principalTable: "CreditCardPurchases",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditCardInstallments_CreditCards_TenantId_UserId_CardId",
                        columns: x => new { x.TenantId, x.UserId, x.CardId },
                        principalSchema: "app",
                        principalTable: "CreditCards",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditCardInstallments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCards_TenantId_UserId_PreferredPaymentAccountId",
                schema: "app",
                table: "CreditCards",
                columns: new[] { "TenantId", "UserId", "PreferredPaymentAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardInstallments_TenantId_UserId_CardId",
                schema: "app",
                table: "CreditCardInstallments",
                columns: new[] { "TenantId", "UserId", "CardId" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardInstallments_TenantId_UserId_InvoiceId_Competence",
                schema: "app",
                table: "CreditCardInstallments",
                columns: new[] { "TenantId", "UserId", "InvoiceId", "Competence" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardInstallments_TenantId_UserId_PurchaseId_Number",
                schema: "app",
                table: "CreditCardInstallments",
                columns: new[] { "TenantId", "UserId", "PurchaseId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardInvoices_TenantId_UserId_CardId_Competence",
                schema: "app",
                table: "CreditCardInvoices",
                columns: new[] { "TenantId", "UserId", "CardId", "Competence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardPurchases_TenantId_UserId_CardId_PurchasedOn_Id",
                schema: "app",
                table: "CreditCardPurchases",
                columns: new[] { "TenantId", "UserId", "CardId", "PurchasedOn", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardPurchases_TenantId_UserId_CategoryId",
                schema: "app",
                table: "CreditCardPurchases",
                columns: new[] { "TenantId", "UserId", "CategoryId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CreditCards_Accounts_TenantId_UserId_PreferredPaymentAccoun~",
                schema: "app",
                table: "CreditCards",
                columns: new[] { "TenantId", "UserId", "PreferredPaymentAccountId" },
                principalSchema: "app",
                principalTable: "Accounts",
                principalColumns: new[] { "TenantId", "UserId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditCards_Accounts_TenantId_UserId_PreferredPaymentAccoun~",
                schema: "app",
                table: "CreditCards");

            migrationBuilder.DropTable(
                name: "CreditCardInstallments",
                schema: "app");

            migrationBuilder.DropTable(
                name: "CreditCardInvoices",
                schema: "app");

            migrationBuilder.DropTable(
                name: "CreditCardPurchases",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_CreditCards_TenantId_UserId_PreferredPaymentAccountId",
                schema: "app",
                table: "CreditCards");

            migrationBuilder.DropColumn(
                name: "AccentColor",
                schema: "app",
                table: "CreditCards");

            migrationBuilder.DropColumn(
                name: "Brand",
                schema: "app",
                table: "CreditCards");

            migrationBuilder.DropColumn(
                name: "Institution",
                schema: "app",
                table: "CreditCards");

            migrationBuilder.DropColumn(
                name: "PreferredPaymentAccountId",
                schema: "app",
                table: "CreditCards");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CreditCards_Days",
                schema: "app",
                table: "CreditCards");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CreditCards_Days",
                schema: "app",
                table: "CreditCards",
                sql: "\"ClosingDay\" BETWEEN 1 AND 28 AND \"DueDay\" BETWEEN 1 AND 28");
        }
    }
}
