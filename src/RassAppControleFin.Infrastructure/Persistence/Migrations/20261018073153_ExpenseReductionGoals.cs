using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RassAppControleFin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpenseReductionGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseReductionGoals",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceAmount = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    TargetAmount = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseReductionGoals", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_ExpenseReductionGoals_Amounts", "\"ReferenceAmount\" > \"TargetAmount\" AND \"TargetAmount\" >= 0");
                    table.CheckConstraint("CK_ExpenseReductionGoals_Currency", "\"Currency\" IN ('BRL','USD','EUR')");
                    table.CheckConstraint("CK_ExpenseReductionGoals_Period", "\"PeriodStart\" < \"PeriodEnd\"");
                    table.CheckConstraint("CK_ExpenseReductionGoals_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_ExpenseReductionGoals_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_ExpenseReductionGoals_Categories_TenantId_UserId_CategoryId",
                        columns: x => new { x.TenantId, x.UserId, x.CategoryId },
                        principalSchema: "app",
                        principalTable: "Categories",
                        principalColumns: new[] { "TenantId", "UserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseReductionGoals_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReductionGoals_TenantId_UserId_CategoryId",
                schema: "app",
                table: "ExpenseReductionGoals",
                columns: new[] { "TenantId", "UserId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReductionGoals_TenantId_UserId_PeriodStart_PeriodEnd",
                schema: "app",
                table: "ExpenseReductionGoals",
                columns: new[] { "TenantId", "UserId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.Sql("""
                ALTER TABLE app."ExpenseReductionGoals" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE app."ExpenseReductionGoals" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_user_scope ON app."ExpenseReductionGoals" FOR ALL USING (
                  "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                  AND "UserId" = NULLIF(current_setting('app.user_id', true), '')
                ) WITH CHECK (
                  "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                  AND "UserId" = NULLIF(current_setting('app.user_id', true), '')
                );
                GRANT SELECT, INSERT, UPDATE ON app."ExpenseReductionGoals" TO rass_app_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseReductionGoals",
                schema: "app");
        }
    }
}
