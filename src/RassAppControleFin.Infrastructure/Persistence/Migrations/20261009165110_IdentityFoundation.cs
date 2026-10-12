using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RassAppControleFin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IdentityFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                schema: "app",
                table: "Tenants",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValueSql: "COALESCE(NULLIF(current_setting('app.user_id', true), ''), 'legacy-unassigned')");
            migrationBuilder.Sql("""
                CREATE TABLE app."TenantMemberships" (
                    "TenantId" uuid NOT NULL REFERENCES app."Tenants"("Id") ON DELETE CASCADE,
                    "UserId" varchar(256) NOT NULL CHECK(length(btrim("UserId"))>0),
                    "Role" varchar(32) NOT NULL CHECK("Role" IN ('User','TenantAdmin','SupportAgent','FinancialAdmin')),
                    "IsActive" boolean NOT NULL, "IsPersonal" boolean NOT NULL DEFAULT false,
                    "CreatedAt" timestamptz NOT NULL, PRIMARY KEY("TenantId","UserId"));
                CREATE UNIQUE INDEX "UX_PersonalTenant" ON app."TenantMemberships"("UserId") WHERE "IsPersonal";
                CREATE INDEX "IX_Membership_User" ON app."TenantMemberships"("UserId","IsActive");
                CREATE TABLE app."UserSessions" (
                    "Id" uuid PRIMARY KEY, "UserId" varchar(256) NOT NULL,
                    "CookieHash" char(64) NOT NULL, "TokenHash" char(64) NOT NULL,
                    "AccessExpiresAt" timestamptz NOT NULL, "ExpiresAt" timestamptz NOT NULL,
                    "CreatedAt" timestamptz NOT NULL, "RevokedAt" timestamptz);
                CREATE INDEX "IX_Sessions_User" ON app."UserSessions"("UserId","ExpiresAt") WHERE "RevokedAt" IS NULL;
                CREATE TABLE app."AuditEntries" (
                    "Id" uuid PRIMARY KEY, "UserId" varchar(256) NOT NULL,
                    "TenantId" uuid, "Action" varchar(64) NOT NULL, "ResourceId" uuid NOT NULL,
                    "Outcome" varchar(32) NOT NULL, "CorrelationId" varchar(128) NOT NULL, "CreatedAt" timestamptz NOT NULL);
                CREATE INDEX "IX_Audit_Scope" ON app."AuditEntries"("TenantId","UserId","CreatedAt");
                ALTER TABLE app."TenantMemberships" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE app."TenantMemberships" FORCE ROW LEVEL SECURITY;
                ALTER TABLE app."UserSessions" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE app."UserSessions" FORCE ROW LEVEL SECURITY;
                ALTER TABLE app."AuditEntries" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE app."AuditEntries" FORCE ROW LEVEL SECURITY;
                CREATE POLICY membership_read ON app."TenantMemberships" FOR SELECT
                    USING ("UserId"=NULLIF(current_setting('app.user_id',true),''));
                CREATE POLICY membership_insert ON app."TenantMemberships" FOR INSERT WITH CHECK (
                    "TenantId"=NULLIF(current_setting('app.tenant_id',true),'')::uuid
                    AND EXISTS (SELECT 1 FROM app."Tenants" t WHERE t."Id"="TenantId"
                        AND t."OwnerUserId"=NULLIF(current_setting('app.user_id',true),''))
                    AND (NOT "IsPersonal" OR "UserId"=NULLIF(current_setting('app.user_id',true),'')));
                CREATE POLICY session_user_scope ON app."UserSessions" FOR ALL
                    USING ("UserId"=NULLIF(current_setting('app.user_id',true),''))
                    WITH CHECK ("UserId"=NULLIF(current_setting('app.user_id',true),''));
                CREATE POLICY audit_read ON app."AuditEntries" FOR SELECT USING (
                    "UserId"=NULLIF(current_setting('app.user_id',true),'')
                    AND ("TenantId" IS NULL OR "TenantId"=NULLIF(current_setting('app.tenant_id',true),'')::uuid));
                CREATE POLICY audit_insert ON app."AuditEntries" FOR INSERT WITH CHECK (
                    "UserId"=NULLIF(current_setting('app.user_id',true),'')
                    AND ("TenantId" IS NULL OR "TenantId"=NULLIF(current_setting('app.tenant_id',true),'')::uuid));
                ALTER POLICY tenant_scope ON app."Tenants" USING (
                    "Id"=NULLIF(current_setting('app.tenant_id',true),'')::uuid
                    AND ("OwnerUserId"=NULLIF(current_setting('app.user_id',true),'')
                        OR EXISTS (SELECT 1 FROM app."TenantMemberships" m
                            WHERE m."TenantId"="Id" AND m."UserId"=NULLIF(current_setting('app.user_id',true),'') AND m."IsActive")))
                    WITH CHECK (
                    "Id"=NULLIF(current_setting('app.tenant_id',true),'')::uuid
                    AND "OwnerUserId"=NULLIF(current_setting('app.user_id',true),''));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER POLICY tenant_scope ON app."Tenants" USING (
                    "Id"=NULLIF(current_setting('app.tenant_id',true),'')::uuid
                    AND NULLIF(current_setting('app.user_id',true),'') IS NOT NULL) WITH CHECK (
                    "Id"=NULLIF(current_setting('app.tenant_id',true),'')::uuid
                    AND NULLIF(current_setting('app.user_id',true),'') IS NOT NULL);
                DROP TABLE app."AuditEntries", app."UserSessions", app."TenantMemberships";
                """);
            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "app",
                table: "Tenants");
        }
    }
}
