# V2-03 Phase F — Refresh token foundation

- 256-bit cryptographically random opaque refresh token, base64url encoded.
- SHA-256 digest of refresh token is the only value permitted in persistent storage.
- User session persistence boundary requires atomic create, rotate and revoke operations.
- Rotation must ensure single-use semantics, reject expired/revoked tokens, and revoke the entire family on replay.
- UserSession and RefreshToken tables already exist in V2-02; no schema change is made.

**Not implemented:** SQL transactional IUserSessionStore, refresh endpoint, cookie/response transport policy, rate limits, audit, session validation middleware. Do not enable login or refresh endpoints until these are completed and tested.
