# V2-03 Phase G — SQL Session Store

Implements IUserSessionStore against existing V2-02 identity.UserSession and identity.RefreshToken tables.

- CreateAsync writes session and first hashed refresh token in a single SERIALIZABLE transaction.
- RotateAsync uses UPDLOCK/HOLDLOCK, creates a replacement token, and marks the old token used in one transaction.
- Reuse of a previously used token revokes the session and refresh-token family atomically.
- RevokeAsync marks session and all its tokens revoked in one transaction.
- Token hashes only; no raw refresh tokens are persisted.

## Verification gate
Run SQL Server integration tests for create, rotate, concurrent rotate, replay, expiry, revocation and rollback before enabling HTTP endpoints. SQL Server must be the isolated V2 database. The schema's TokenHash uniqueness and transactional deadlock behavior must be reviewed under concurrency. No real DB has been accessed from this PR.

## Remaining work
Register the store via DI using secret-managed connection strings; implement atomic login lockout/audit, JWT bearer validation with session checks, login/refresh/logout endpoints, and rate limits. No schema changes or auto-merge.
