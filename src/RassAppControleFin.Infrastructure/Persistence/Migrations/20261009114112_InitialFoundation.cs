using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RassAppControleFin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "app");

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                    table.CheckConstraint("CK_Tenants_Name", "length(btrim(\"Name\")) BETWEEN 1 AND 120");
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_UserProfiles_DisplayName", "length(btrim(\"DisplayName\")) BETWEEN 1 AND 120");
                    table.CheckConstraint("CK_UserProfiles_UserId", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_UserProfiles_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_UserProfiles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_TenantId_UserId",
                schema: "app",
                table: "UserProfiles",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.Sql("""
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProfiles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "app");
        }
    }
}
