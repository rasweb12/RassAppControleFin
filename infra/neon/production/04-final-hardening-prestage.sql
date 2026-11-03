-- Checkpoint 14 final-hardening pre-stage.
-- WRITE OPERATION: execute only after the approved 11-17 post-validation,
-- immediately before migrations 18-20, as neondb_owner on Production.
--
-- Migration 20 asserts that the runtime cannot read EF history. Legacy
-- Production grants that predate the migration can contain that SELECT, so
-- remove only that privilege before invoking the EF final-hardening stage.

DO $$
BEGIN
  IF current_database() <> 'neondb'
     OR current_user <> 'neondb_owner'
     OR NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_app_runtime_v2') THEN
    RAISE EXCEPTION 'Checkpoint 14 pre-stage requires neondb_owner on the approved Production database with the runtime role present';
  END IF;
END $$;

REVOKE SELECT ON TABLE app."__EFMigrationsHistory" FROM rass_app_runtime_v2;

DO $$
BEGIN
  IF has_table_privilege('rass_app_runtime_v2', 'app."__EFMigrationsHistory"', 'SELECT') THEN
    RAISE EXCEPTION 'Runtime SELECT on EF migrations history remains after revocation';
  END IF;
END $$;
