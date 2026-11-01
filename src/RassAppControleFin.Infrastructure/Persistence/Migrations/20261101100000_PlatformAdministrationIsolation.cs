using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RassAppControleFin.Infrastructure.Persistence.Migrations;

/// <summary>
/// Separates global-role mutation from the financial runtime. The target role is
/// provisioned by the operational runbook before this migration is applied.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261101100000_PlatformAdministrationIsolation")]
public sealed class PlatformAdministrationIsolation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$ BEGIN
              IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_platform_admin_v1')
                 OR NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_platform_executor_owner_v1') THEN
                RAISE EXCEPTION 'Platform administration roles must be provisioned before PlatformAdministrationIsolation';
              END IF;
            END $$;

            REVOKE INSERT, UPDATE ON app."PlatformRoleAssignments" FROM rass_app_runtime_v2;
            GRANT SELECT ON app."PlatformRoleAssignments" TO rass_app_runtime_v2;

            DROP POLICY IF EXISTS platform_assignment_self_read ON app."PlatformRoleAssignments";
            DROP POLICY IF EXISTS platform_assignment_admin_insert ON app."PlatformRoleAssignments";
            DROP POLICY IF EXISTS platform_assignment_admin_update ON app."PlatformRoleAssignments";

            CREATE POLICY platform_assignment_runtime_self_read ON app."PlatformRoleAssignments"
              FOR SELECT TO rass_app_runtime_v2
              USING ("UserId" = NULLIF(current_setting('app.user_id', true), ''));
            CREATE POLICY platform_assignment_executor_read ON app."PlatformRoleAssignments"
              FOR SELECT TO rass_platform_executor_owner_v1 USING (true);
            CREATE POLICY platform_assignment_executor_insert ON app."PlatformRoleAssignments"
              FOR INSERT TO rass_platform_executor_owner_v1 WITH CHECK (true);
            CREATE POLICY platform_assignment_executor_update ON app."PlatformRoleAssignments"
              FOR UPDATE TO rass_platform_executor_owner_v1 USING (true) WITH CHECK (true);
            CREATE POLICY platform_executor_audit_insert ON app."AuditEntries"
              FOR INSERT TO rass_platform_executor_owner_v1 WITH CHECK (true);
            CREATE POLICY platform_executor_tenant_read ON app."Tenants"
              FOR SELECT TO rass_platform_executor_owner_v1 USING (true);
            CREATE POLICY platform_executor_profile_read ON app."UserProfiles"
              FOR SELECT TO rass_platform_executor_owner_v1 USING (true);

            GRANT USAGE ON SCHEMA app TO rass_platform_executor_owner_v1, rass_platform_admin_v1;
            GRANT SELECT, INSERT, UPDATE ON app."PlatformRoleAssignments", app."AuditEntries" TO rass_platform_executor_owner_v1;
            GRANT SELECT ON app."Tenants", app."UserProfiles" TO rass_platform_executor_owner_v1;

            CREATE FUNCTION app.platform_dashboard()
            RETURNS TABLE(users bigint, tenants bigint, super_admins bigint)
            LANGUAGE sql SECURITY DEFINER SET search_path = pg_catalog, app AS $$
              SELECT (SELECT count(DISTINCT "UserId") FROM app."UserProfiles"),
                     (SELECT count(*) FROM app."Tenants"),
                     (SELECT count(*) FROM app."PlatformRoleAssignments" WHERE "IsActive" AND "Role"='SuperAdmin');
            $$;
            CREATE FUNCTION app.platform_roles(p_user_id text)
            RETURNS TABLE(id uuid, user_id text, role text, is_active boolean, granted_by text, granted_at timestamptz, revoked_by text, revoked_at timestamptz, reason text)
            LANGUAGE sql SECURITY DEFINER SET search_path = pg_catalog, app AS $$
              SELECT "Id","UserId","Role","IsActive","GrantedBy","GrantedAt","RevokedBy","RevokedAt","Reason"
              FROM app."PlatformRoleAssignments" WHERE "UserId"=p_user_id ORDER BY "CreatedAt" DESC;
            $$;
            CREATE FUNCTION app.platform_grant_superadmin(p_assignment_id uuid, p_audit_id uuid, p_actor text, p_target text, p_reason text, p_correlation text)
            RETURNS TABLE(id uuid, granted_at timestamptz)
            LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog, app AS $$
            DECLARE v_now timestamptz := clock_timestamp();
            BEGIN
              IF p_actor IS NULL OR p_target IS NULL OR p_actor='' OR p_target='' OR p_actor=p_target OR length(p_reason) NOT BETWEEN 1 AND 500 THEN
                RAISE EXCEPTION 'platform role request rejected' USING ERRCODE='P0001';
              END IF;
              PERFORM pg_advisory_xact_lock(hashtext('rass.platform.superadmin'));
              INSERT INTO app."PlatformRoleAssignments" ("Id","UserId","Role","IsActive","GrantedBy","GrantedAt","Reason","CreatedAt","UpdatedAt")
              VALUES (p_assignment_id,p_target,'SuperAdmin',true,p_actor,v_now,btrim(p_reason),v_now,v_now)
              ON CONFLICT ("UserId","Role") WHERE "IsActive" DO NOTHING
              RETURNING "Id","GrantedAt" INTO id,granted_at;
              IF id IS NULL THEN RAISE EXCEPTION 'platform role conflict' USING ERRCODE='P0001'; END IF;
              INSERT INTO app."AuditEntries" ("Id","UserId","TenantId","Action","ResourceId","Outcome","CorrelationId","CreatedAt")
              VALUES (p_audit_id,p_actor,NULL,'platform.role.granted',id,'success',p_correlation,v_now);
              RETURN NEXT;
            END;
            $$;
            CREATE FUNCTION app.platform_revoke_superadmin(p_audit_id uuid, p_actor text, p_target text, p_assignment_id uuid, p_reason text, p_correlation text)
            RETURNS boolean
            LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog, app AS $$
            DECLARE v_user text; v_active boolean; v_count bigint; v_now timestamptz := clock_timestamp();
            BEGIN
              IF p_actor IS NULL OR p_target IS NULL OR p_actor='' OR p_target='' OR p_actor=p_target OR length(p_reason) NOT BETWEEN 1 AND 500 THEN RETURN false; END IF;
              PERFORM pg_advisory_xact_lock(hashtext('rass.platform.superadmin'));
              SELECT "UserId","IsActive",(SELECT count(*) FROM app."PlatformRoleAssignments" WHERE "IsActive" AND "Role"='SuperAdmin')
                INTO v_user,v_active,v_count FROM app."PlatformRoleAssignments" WHERE "Id"=p_assignment_id FOR UPDATE;
              IF NOT FOUND OR v_user<>p_target OR NOT v_active OR v_count<=1 THEN RETURN false; END IF;
              UPDATE app."PlatformRoleAssignments" SET "IsActive"=false,"RevokedBy"=p_actor,"RevokedAt"=v_now,"Reason"=btrim(p_reason),"UpdatedAt"=v_now
                WHERE "Id"=p_assignment_id AND "UserId"=p_target AND "IsActive";
              INSERT INTO app."AuditEntries" ("Id","UserId","TenantId","Action","ResourceId","Outcome","CorrelationId","CreatedAt")
              VALUES (p_audit_id,p_actor,NULL,'platform.role.revoked',p_assignment_id,'success',p_correlation,v_now);
              RETURN true;
            END;
            $$;
            ALTER FUNCTION app.platform_dashboard() OWNER TO rass_platform_executor_owner_v1;
            ALTER FUNCTION app.platform_roles(text) OWNER TO rass_platform_executor_owner_v1;
            ALTER FUNCTION app.platform_grant_superadmin(uuid,uuid,text,text,text,text) OWNER TO rass_platform_executor_owner_v1;
            ALTER FUNCTION app.platform_revoke_superadmin(uuid,text,text,uuid,text,text) OWNER TO rass_platform_executor_owner_v1;
            REVOKE ALL ON FUNCTION app.platform_dashboard(), app.platform_roles(text), app.platform_grant_superadmin(uuid,uuid,text,text,text,text), app.platform_revoke_superadmin(uuid,text,text,uuid,text,text) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION app.platform_dashboard(), app.platform_roles(text), app.platform_grant_superadmin(uuid,uuid,text,text,text,text), app.platform_revoke_superadmin(uuid,text,text,uuid,text,text) TO rass_platform_admin_v1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$ BEGIN
              RAISE EXCEPTION 'PlatformAdministrationIsolation cannot be automatically downgraded because it would restore insecure runtime write access. Follow the documented forward-fix rollback procedure.';
            END $$;
            """);
    }
}
