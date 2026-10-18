# Flutter Offline Synchronization Audit

Date: 2026-10-17

The pull flow previously recorded markers and advanced its cursor without applying financial data to Drift. It now retrieves authorized API snapshots and atomically persists snapshots, inbox markers, and the applied cursor.

The API now returns `appliedCursor` on every page, including the final page. Unknown events fail without cursor advancement. Pending local `local-*` resources remain visible, and inbox insertion remains idempotent per subject, tenant, and operation.

Validation: `flutter analyze` passed; `flutter test` passed (19 tests). Real Neon sync integration was not run because its fixture mutates the isolated development database and needs approval.
