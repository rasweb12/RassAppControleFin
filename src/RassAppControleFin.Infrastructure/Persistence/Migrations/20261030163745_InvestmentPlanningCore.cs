using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RassAppControleFin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InvestmentPlanningCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmergencyFundPlans",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    TargetMultiple = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    TargetAmount = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    CurrentAmount = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    MonthlyContributionTarget = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    LiquidityRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyFundPlans", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_EmergencyFundPlans_Amounts", "\"TargetAmount\" > 0 AND \"CurrentAmount\" >= 0 AND \"MonthlyContributionTarget\" >= 0");
                    table.CheckConstraint("CK_EmergencyFundPlans_Currency", "\"Currency\" IN ('BRL','USD','EUR')");
                    table.CheckConstraint("CK_EmergencyFundPlans_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_EmergencyFundPlans_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_EmergencyFundPlans_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvestmentAssets",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AssetClass = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AssetSubClass = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Issuer = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Indexer = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(12,8)", precision: 12, scale: 8, nullable: true),
                    MaturityDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LiquidityType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FgcProtected = table.Column<bool>(type: "boolean", nullable: false),
                    TaxExempt = table.Column<bool>(type: "boolean", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentAssets", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_InvestmentAssets_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_InvestmentAssets_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_InvestmentAssets_Accounts_TenantId_UserId_AccountId",
                        columns: x => new { x.TenantId, x.UserId, x.AccountId },
                        principalSchema: "app",
                        principalTable: "Accounts",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvestmentAssets_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayYourselfFirstPlans",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CurrentRate = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    GoalRate = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayYourselfFirstPlans", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_PayYourselfFirstPlans_Rates", "\"CurrentRate\" BETWEEN 0 AND 1 AND (\"GoalRate\" IS NULL OR \"GoalRate\" BETWEEN 0 AND 1)");
                    table.CheckConstraint("CK_PayYourselfFirstPlans_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_PayYourselfFirstPlans_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_PayYourselfFirstPlans_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvestmentTransactions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    InvestmentAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(22,8)", precision: 22, scale: 8, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(16,8)", precision: 16, scale: 8, nullable: true),
                    GrossAmount = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    Fees = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    Taxes = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentTransactions", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_InvestmentTransactions_Amounts", "\"GrossAmount\" >= 0 AND \"Fees\" >= 0 AND \"Taxes\" >= 0 AND \"NetAmount\" = \"GrossAmount\" - \"Fees\" - \"Taxes\"");
                    table.CheckConstraint("CK_InvestmentTransactions_Currency", "\"Currency\" IN ('BRL','USD','EUR')");
                    table.CheckConstraint("CK_InvestmentTransactions_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_InvestmentTransactions_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_InvestmentTransactions_InvestmentAssets_TenantId_UserId_Inv~",
                        columns: x => new { x.TenantId, x.UserId, x.InvestmentAssetId },
                        principalSchema: "app",
                        principalTable: "InvestmentAssets",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvestmentTransactions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TargetPortfolioAllocations",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    InvestmentAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AssetClass = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetPortfolioAllocations", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_TargetPortfolioAllocations_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_TargetPortfolioAllocations_Version", "\"Version\" > 0");
                    table.CheckConstraint("CK_TargetPortfolioAllocations_Weight", "\"Weight\" BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_TargetPortfolioAllocations_InvestmentAssets_TenantId_UserId~",
                        columns: x => new { x.TenantId, x.UserId, x.InvestmentAssetId },
                        principalSchema: "app",
                        principalTable: "InvestmentAssets",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TargetPortfolioAllocations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayYourselfFirstPlanRevisions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentRate = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    GoalRate = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayYourselfFirstPlanRevisions", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_PayYourselfFirstPlanRevisions_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_PayYourselfFirstPlanRevisions_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_PayYourselfFirstPlanRevisions_PayYourselfFirstPlans_TenantI~",
                        columns: x => new { x.TenantId, x.UserId, x.PlanId },
                        principalSchema: "app",
                        principalTable: "PayYourselfFirstPlans",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayYourselfFirstPlanRevisions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyFundPlans_TenantId_UserId",
                schema: "app",
                table: "EmergencyFundPlans",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentAssets_TenantId_UserId_AccountId",
                schema: "app",
                table: "InvestmentAssets",
                columns: new[] { "TenantId", "UserId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentAssets_TenantId_UserId_Active_AssetClass",
                schema: "app",
                table: "InvestmentAssets",
                columns: new[] { "TenantId", "UserId", "Active", "AssetClass" });

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentTransactions_TenantId_UserId_InvestmentAssetId_Da~",
                schema: "app",
                table: "InvestmentTransactions",
                columns: new[] { "TenantId", "UserId", "InvestmentAssetId", "Date", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_PayYourselfFirstPlanRevisions_TenantId_UserId_PlanId_Effect~",
                schema: "app",
                table: "PayYourselfFirstPlanRevisions",
                columns: new[] { "TenantId", "UserId", "PlanId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayYourselfFirstPlans_TenantId_UserId",
                schema: "app",
                table: "PayYourselfFirstPlans",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TargetPortfolioAllocations_TenantId_UserId_InvestmentAssetId",
                schema: "app",
                table: "TargetPortfolioAllocations",
                columns: new[] { "TenantId", "UserId", "InvestmentAssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_TargetPortfolioAllocations_TenantId_UserId_Key",
                schema: "app",
                table: "TargetPortfolioAllocations",
                columns: new[] { "TenantId", "UserId", "Key" },
                unique: true);

            migrationBuilder.Sql("""
                ALTER TABLE app."PayYourselfFirstPlans" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE app."PayYourselfFirstPlans" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_user_scope ON app."PayYourselfFirstPlans" FOR ALL USING (
                  "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                  AND "UserId" = NULLIF(current_setting('app.user_id', true), '')
                ) WITH CHECK (
                  "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                  AND "UserId" = NULLIF(current_setting('app.user_id', true), '')
                );
                ALTER TABLE app."PayYourselfFirstPlanRevisions" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE app."PayYourselfFirstPlanRevisions" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_user_scope ON app."PayYourselfFirstPlanRevisions" FOR ALL USING (
                  "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                  AND "UserId" = NULLIF(current_setting('app.user_id', true), '')
                ) WITH CHECK (
                  "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                  AND "UserId" = NULLIF(current_setting('app.user_id', true), '')
                );
                ALTER TABLE app."EmergencyFundPlans" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE app."EmergencyFundPlans" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_user_scope ON app."EmergencyFundPlans" FOR ALL USING (
                  "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                  AND "UserId" = NULLIF(current_setting('app.user_id', true), '')
                ) WITH CHECK (
                  "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                  AND "UserId" = NULLIF(current_setting('app.user_id', true), '')
                );
                ALTER TABLE app."InvestmentAssets" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE app."InvestmentAssets" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_user_scope ON app."InvestmentAssets" FOR ALL USING (
                  "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                  AND "UserId" = NULLIF(current_setting('app.user_id', true), '')
                ) WITH CHECK (
                  "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                  AND "UserId" = NULLIF(current_setting('app.user_id', true), '')
                );
                ALTER TABLE app."InvestmentTransactions" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE app."InvestmentTransactions" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_user_scope ON app."InvestmentTransactions" FOR ALL USING (
                  "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                  AND "UserId" = NULLIF(current_setting('app.user_id', true), '')
                ) WITH CHECK (
                  "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                  AND "UserId" = NULLIF(current_setting('app.user_id', true), '')
                );
                ALTER TABLE app."TargetPortfolioAllocations" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE app."TargetPortfolioAllocations" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_user_scope ON app."TargetPortfolioAllocations" FOR ALL USING (
                  "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                  AND "UserId" = NULLIF(current_setting('app.user_id', true), '')
                ) WITH CHECK (
                  "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                  AND "UserId" = NULLIF(current_setting('app.user_id', true), '')
                );
                GRANT SELECT, INSERT, UPDATE ON app."PayYourselfFirstPlans", app."EmergencyFundPlans",
                  app."InvestmentAssets", app."TargetPortfolioAllocations" TO rass_app_runtime_v2;
                GRANT SELECT, INSERT ON app."PayYourselfFirstPlanRevisions", app."InvestmentTransactions" TO rass_app_runtime_v2;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmergencyFundPlans",
                schema: "app");

            migrationBuilder.DropTable(
                name: "InvestmentTransactions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "PayYourselfFirstPlanRevisions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "TargetPortfolioAllocations",
                schema: "app");

            migrationBuilder.DropTable(
                name: "PayYourselfFirstPlans",
                schema: "app");

            migrationBuilder.DropTable(
                name: "InvestmentAssets",
                schema: "app");
        }
    }
}
