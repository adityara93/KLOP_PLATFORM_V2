# V2-03 Phase H — Lockout and Authentication Audit

Adds ILoginSecurityStore and SQL implementation using V2-02 existing identity.UserCredential and audit.AuditLog tables.

- Five failed password attempts trigger a 15-minute lockout.
- Atomic SQL update increments the failure count and writes a FAILED audit entry in the same transaction.
- Success resets the counter and lockout and writes a SUCCESS audit entry in one transaction.
- No passwords, hashes, JWTs or refresh tokens are written to audit.

**Integration gate:** The existing PasswordAuthenticationService does not yet invoke this store. It must be composed with lockout enforcement and session issuance in a subsequent phase. Audit of unknown-user attempts and rate limiting by IP/account must be implemented at the HTTP boundary. SQL integration tests (concurrent failures, lockout expiration, audit rollback, success race) are required before endpoint activation. Do not mark production ready based on this PR alone.
