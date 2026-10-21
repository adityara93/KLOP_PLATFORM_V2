# V2-03 Authentication API completion

Endpoints: POST /api/auth/login, POST /api/auth/refresh, POST /api/auth/logout (Bearer), GET /api/auth/me (Bearer).

Configuration must be provided via secret-managed environment/configuration (never committed):
- ConnectionStrings__PlatformV2 — SQL Server connection string for KLOP_PLATFORM_V2_DB
- Jwt__Issuer — expected token issuer
- Jwt__Audience — expected token audience
- Jwt__SigningKey — minimum 32-byte random secret
- Jwt__AccessTokenMinutes — 1 to 60 (default 15)

Login request: {"username":"admin@PLATFORM","password":"..."} or {"username":"user@TENANTCODE","password":"..."}.
Refresh request: {"refreshToken":"..."}.
Successful login/refresh returns accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt and sessionId.
Never log raw credentials or refresh tokens.

Security: JWT signature, issuer, audience, expiry and live session/user/tenant validation on protected requests; 10 requests/minute per IP on login and refresh. Session revocation on logout.

**Before production approval:** run real SQL Server integration tests including concurrent login lockout, refresh rotation/replay, token expiry and logout. Review refresh-token family expiry behavior and account status race during refresh. Confirm reverse-proxy forwarded IP configuration, HTTPS and secret management. No seed credentials are provided.
