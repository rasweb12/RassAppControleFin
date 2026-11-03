using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RassAppControleFin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MonetizationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentEvents",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderEventId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PurchaseFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProductId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanDefinitions",
                schema: "app",
                columns: table => new
                {
                    Plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ReferencePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    OneTime = table.Column<bool>(type: "boolean", nullable: false),
                    StoreProductId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanDefinitions", x => x.Plan);
                    table.CheckConstraint("CK_PlanDefinitions_Price", "\"ReferencePrice\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "PlanEntitlements",
                schema: "app",
                columns: table => new
                {
                    Plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Feature = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Limit = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanEntitlements", x => new { x.Plan, x.Feature });
                    table.CheckConstraint("CK_PlanEntitlements_Limit", "\"Limit\" IS NULL OR \"Limit\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ExternalPurchaseId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentEvents_Provider_ProviderEventId",
                schema: "app",
                table: "PaymentEvents",
                columns: new[] { "Provider", "ProviderEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentEvents_Provider_PurchaseFingerprint",
                schema: "app",
                table: "PaymentEvents",
                columns: new[] { "Provider", "PurchaseFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanEntitlements_Feature",
                schema: "app",
                table: "PlanEntitlements",
                column: "Feature");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_Provider_ExternalPurchaseId",
                schema: "app",
                table: "UserSubscriptions",
                columns: new[] { "Provider", "ExternalPurchaseId" },
                unique: true,
                filter: "\"ExternalPurchaseId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId_Status",
                schema: "app",
                table: "UserSubscriptions",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_app_runtime_v2')
                     OR EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_app_runtime_v2'
                        AND (rolsuper OR rolbypassrls OR rolcreatedb OR rolcreaterole OR rolreplication OR rolinherit)) THEN
                    RAISE EXCEPTION 'Unsafe or missing runtime role';
                  END IF;
                END $$;

                REVOKE ALL ON app."PlanDefinitions", app."PlanEntitlements", app."UserSubscriptions", app."PaymentEvents" FROM PUBLIC;
                REVOKE ALL ON app."PlanDefinitions", app."PlanEntitlements", app."UserSubscriptions", app."PaymentEvents" FROM rass_app_runtime_v2;
                GRANT SELECT ON app."PlanDefinitions", app."PlanEntitlements", app."UserSubscriptions" TO rass_app_runtime_v2;

                -- Paid offers start disabled: changing this catalogue never creates or changes a Play product.
                INSERT INTO app."PlanDefinitions" ("Plan","DisplayName","ReferencePrice","Currency","Enabled","OneTime","StoreProductId","UpdatedAt") VALUES
                  ('Free','Free',0,'BRL',true,false,NULL,now()),
                  ('PremiumMonthly','Premium Mensal',14.90,'BRL',false,false,NULL,now()),
                  ('PremiumAnnual','Premium Anual',119.90,'BRL',false,false,NULL,now()),
                  ('PremiumLifetime','Premium Vitalicio',299.90,'BRL',false,true,NULL,now())
                ON CONFLICT ("Plan") DO NOTHING;

                INSERT INTO app."PlanEntitlements" ("Plan","Feature","Enabled","Limit","UpdatedAt") VALUES
                  ('Free','accounts',true,2,now()), ('Free','credit_cards',true,1,now()),
                  ('Free','planning',true,NULL,now()), ('Free','investments',true,NULL,now()),
                  ('Free','pay_yourself_first',true,NULL,now()), ('Free','emergency_fund',true,1,now()),
                  ('Free','projections',false,NULL,now()), ('Free','advanced_reports',false,NULL,now()),
                  ('PremiumMonthly','accounts',true,NULL,now()), ('PremiumMonthly','credit_cards',true,NULL,now()),
                  ('PremiumMonthly','planning',true,NULL,now()), ('PremiumMonthly','investments',true,NULL,now()),
                  ('PremiumMonthly','pay_yourself_first',true,NULL,now()), ('PremiumMonthly','emergency_fund',true,NULL,now()),
                  ('PremiumMonthly','projections',true,NULL,now()), ('PremiumMonthly','advanced_reports',true,NULL,now()),
                  ('PremiumAnnual','accounts',true,NULL,now()), ('PremiumAnnual','credit_cards',true,NULL,now()),
                  ('PremiumAnnual','planning',true,NULL,now()), ('PremiumAnnual','investments',true,NULL,now()),
                  ('PremiumAnnual','pay_yourself_first',true,NULL,now()), ('PremiumAnnual','emergency_fund',true,NULL,now()),
                  ('PremiumAnnual','projections',true,NULL,now()), ('PremiumAnnual','advanced_reports',true,NULL,now()),
                  ('PremiumLifetime','accounts',true,NULL,now()), ('PremiumLifetime','credit_cards',true,NULL,now()),
                  ('PremiumLifetime','planning',true,NULL,now()), ('PremiumLifetime','investments',true,NULL,now()),
                  ('PremiumLifetime','pay_yourself_first',true,NULL,now()), ('PremiumLifetime','emergency_fund',true,NULL,now()),
                  ('PremiumLifetime','projections',true,NULL,now()), ('PremiumLifetime','advanced_reports',true,NULL,now())
                ON CONFLICT ("Plan","Feature") DO NOTHING;

                ALTER TABLE app."PlanDefinitions" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE app."PlanDefinitions" FORCE ROW LEVEL SECURITY;
                CREATE POLICY plan_definition_read ON app."PlanDefinitions" FOR SELECT TO rass_app_runtime_v2 USING (true);
                ALTER TABLE app."PlanEntitlements" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE app."PlanEntitlements" FORCE ROW LEVEL SECURITY;
                CREATE POLICY plan_entitlement_read ON app."PlanEntitlements" FOR SELECT TO rass_app_runtime_v2 USING (true);
                ALTER TABLE app."UserSubscriptions" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE app."UserSubscriptions" FORCE ROW LEVEL SECURITY;
                CREATE POLICY subscription_user_scope ON app."UserSubscriptions" FOR SELECT TO rass_app_runtime_v2
                  USING ("UserId" = NULLIF(current_setting('app.user_id', true), ''));
                ALTER TABLE app."PaymentEvents" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE app."PaymentEvents" FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                  IF has_table_privilege('rass_app_runtime_v2', 'app."PlanDefinitions"', 'INSERT,UPDATE,DELETE')
                     OR has_table_privilege('rass_app_runtime_v2', 'app."PlanEntitlements"', 'INSERT,UPDATE,DELETE')
                     OR has_table_privilege('rass_app_runtime_v2', 'app."UserSubscriptions"', 'INSERT,UPDATE,DELETE')
                     OR has_table_privilege('rass_app_runtime_v2', 'app."PaymentEvents"', 'SELECT,INSERT,UPDATE,DELETE') THEN
                    RAISE EXCEPTION 'Runtime monetization ACL is invalid';
                  END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DO $$ BEGIN RAISE EXCEPTION 'MonetizationFoundation is forward-only; use a reviewed forward fix.'; END $$;""");
        }
    }
}
