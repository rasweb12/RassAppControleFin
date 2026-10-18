# Security and Authentication Audit

Date: 2026-10-17

The API authenticates with JWT plus application-session validation and does not explicitly configure Data Protection. The framework warning is not part of mobile JWT authentication.

The Blazor Admin uses cookies, a disk ticket store, and a persistent key ring under `SessionStore:Path`; Render maps it to `/var/data/sessions`. On Linux this key ring has no at-rest encryptor. Before treating Admin session persistence as production-ready, add a reviewed master-key protector from a Render secret with rotation and recovery procedures. Never store it in Git.

Also pending: validate Neon Auth trusted origin and confirm the Brevo sender/SMTP configuration before production password resets.
