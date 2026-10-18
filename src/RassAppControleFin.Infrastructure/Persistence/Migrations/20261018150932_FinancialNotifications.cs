using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RassAppControleFin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FinancialNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialNotifications",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialNotifications", x => new { x.TenantId, x.UserId, x.Id });
                    table.CheckConstraint("CK_FinancialNotifications_Message", "length(btrim(\"Message\")) BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_FinancialNotifications_Title", "length(btrim(\"Title\")) BETWEEN 1 AND 160");
                    table.CheckConstraint("CK_FinancialNotifications_Type", "length(btrim(\"Type\")) BETWEEN 1 AND 64");
                    table.CheckConstraint("CK_FinancialNotifications_User", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_FinancialNotifications_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_FinancialNotifications_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialNotifications_TenantId_UserId_ReadAt_CreatedAt_Id",
                schema: "app",
                table: "FinancialNotifications",
                columns: new[] { "TenantId", "UserId", "ReadAt", "CreatedAt", "Id" });

            migrationBuilder.Sql("""
                ALTER TABLE app."FinancialNotifications" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE app."FinancialNotifications" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_user_scope ON app."FinancialNotifications" FOR ALL USING (
                  "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                  AND "UserId" = NULLIF(current_setting('app.user_id', true), '')
                ) WITH CHECK (
                  "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                  AND "UserId" = NULLIF(current_setting('app.user_id', true), '')
                );
                GRANT SELECT, INSERT, UPDATE ON app."FinancialNotifications" TO rass_app_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialNotifications",
                schema: "app");
        }
    }
}
