using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RassAppControleFin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlatformRbacFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformRoleAssignments",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformRoleAssignments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoleAssignments_Role_IsActive",
                schema: "app",
                table: "PlatformRoleAssignments",
                columns: new[] { "Role", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoleAssignments_UserId_Role",
                schema: "app",
                table: "PlatformRoleAssignments",
                columns: new[] { "UserId", "Role" },
                unique: true,
                filter: "\"IsActive\"");

            migrationBuilder.Sql("""
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("REVOKE ALL ON app.\"PlatformRoleAssignments\" FROM rass_app_runtime_v2;");
            migrationBuilder.DropTable(
                name: "PlatformRoleAssignments",
                schema: "app");
        }
    }
}
