# V2-03 Phase D — Identity credential lookup

Adds a parameterized SQL Server reader for the V2-02 schema. The reader joins user credentials with tenant status, returning a candidate only for a matching tenant code or the reserved PLATFORM administrator.

**Important:** This is not a login endpoint. The reader returns password hashes only to trusted application services. It must never be exposed as an HTTP response. No connection string is hardcoded; the composition root will supply one in a subsequent phase.

Before enabling login, implement atomic failed-login increments and lockout, active account/tenant verification, password hash check, session and refresh token transaction, audit, JWT validation middleware, and rate limiting. Ensure only one PLATFORM admin exists via database constraints.

V2-02 migration on a real SQL Server remains an independent verification gate.
