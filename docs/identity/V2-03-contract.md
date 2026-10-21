# V2-03 Identity & JWT — Phase A

## Login contract
Request: `{ "username": "operator@100002", "password": "<redacted>" }`. PLATFORM administrator: `admin@PLATFORM`.

The parser validates the syntactic identifier only. The authentication service **must** resolve tenant from trusted database records, verify password hash with a modern password hasher, validate active tenant/user, and establish scope server-side. Do not trust the suffix for authorization. No Azure SSO until unblocked.

## Planned endpoints (not yet implemented)
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/me`

## Security gates before endpoints
- JWT signing key from secret store/config; issuer, audience, short expiration; no hardcoded keys
- Refresh tokens: hash-at-rest, rotation, replay detection, revocation on logout
- Rate limits and lockout for failed login
- Platform scope implies null tenant; tenant scope requires trusted tenant FK
- Never log password, JWT or refresh token; audit successful and failed attempts without secrets
- Tests: tenant isolation, platform admin identity, case normalization, invalid formats, lockout, replay

This PR only introduces pure contracts; **no login endpoint is active**. Database migration verification remains an independent V2-02 gate.
