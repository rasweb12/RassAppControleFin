# Render and Neon Audit

Date: 2026-10-17

## Confirmed

- The public API responds successfully to `/health/live` and `/health/ready`.
- Render runs in Oregon while Neon runs in Sao Paulo; measure this latency before scaling.
- The service slug remains `rassappcontrolefin-1`; a service rename did not change the URL.
- The Neon development branch contains the `app` schema and EF migrations. Neon production contains only `neon_auth` tables and cannot satisfy financial readiness.
- The remote Render service has no health-check path while the blueprint specifies `/health/ready`.

## Required remote actions

1. Set the Render health-check path to `/health/ready`.
2. Confirm the connection targets the intended branch with `rass_app_runtime` and TLS `VerifyFull`.
3. Approve a separate production promotion: backup, schema/migrations/RLS, grants, readiness verification, rollback.
4. Rotate the previously exposed runtime password and update secrets without logging it.

No remote mutation was performed.
