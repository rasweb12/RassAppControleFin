using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RassAppControleFin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlanningAndDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Budgets",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_Budgets_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_Budgets_Currency", "\"Currency\" IN ('BRL','USD','EUR')");
                    table.CheckConstraint("CK_Budgets_Period", "\"PeriodStart\" < \"PeriodEnd\"");
                    table.CheckConstraint("CK_Budgets_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_Budgets_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Budgets_Categories_TenantId_UserId_CategoryId",
                        columns: x => new { x.TenantId, x.UserId, x.CategoryId },
                        principalSchema: "app",
                        principalTable: "Categories",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Budgets_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Goals",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TargetAmount = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    CurrentAmount = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TargetDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_Goals_Amounts", "\"TargetAmount\" > 0 AND \"CurrentAmount\" BETWEEN 0 AND \"TargetAmount\"");
                    table.CheckConstraint("CK_Goals_Currency", "\"Currency\" IN ('BRL','USD','EUR')");
                    table.CheckConstraint("CK_Goals_Name", "length(btrim(\"Name\")) BETWEEN 1 AND 120");
                    table.CheckConstraint("CK_Goals_Status", "\"Status\" IN ('Active','Completed','Archived')");
                    table.CheckConstraint("CK_Goals_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_Goals_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Goals_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_TenantId_UserId_CategoryId",
                schema: "app",
                table: "Budgets",
                columns: new[] { "TenantId", "UserId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_TenantId_UserId_PeriodStart_PeriodEnd",
                schema: "app",
                table: "Budgets",
                columns: new[] { "TenantId", "UserId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_Goals_TenantId_UserId_Status",
                schema: "app",
                table: "Goals",
                columns: new[] { "TenantId", "UserId", "Status" });

            migrationBuilder.Sql("""
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Budgets",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Goals",
                schema: "app");
        }
    }
}
