# V2-03 Phase I — Internal authentication orchestration

Composes existing credential lookup, password hashing, login security store, SQL session store, refresh token generator and JWT issuer. On a successful credential check it creates a session and refresh token, writes login success audit, then issues an access token. On password failure it records the failure/lockout. A failure writing the success audit triggers compensating session revocation.

## Mandatory blockers before any HTTP login endpoint

- Session creation and success audit are **not one database transaction**. Compensating revocation is best-effort, not an atomic guarantee. Must unify transaction boundaries.
- Credential state may change between lookup and session creation. Add transactionally enforced lockout/active-state checks and concurrency tests.
- No failed-login audit for unknown users, no anti-enumeration timing strategy, and no IP/account rate limiting.
- Password rehash-on-login not persisted.
- JWT validation middleware and session revocation enforcement are not installed.
- Refresh/logout endpoints and end-to-end SQL Server tests are pending.

This class is internal-only, is not registered in DI and is not mapped to an HTTP route. Do not use for production login until the blockers are closed. No schema changes.
