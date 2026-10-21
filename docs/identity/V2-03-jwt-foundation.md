# V2-03 Phase C — JWT issuer foundation

- HS256 signed access tokens; validated configuration (issuer, audience, key >= 32 bytes, 1-60 minute TTL).
- Claims: sub, jti, sid, scope, tenant_id (tenant only).
- Enforces PLATFORM tenant null and TENANT tenant required when issuing.
- Signing secret must come from a secure configuration provider, never source control.
- This phase is **issuer only**. Do not enable authentication endpoints until token validation middleware, database identity lookup, revocation/session checks, audit and refresh-token rotation are implemented and tested.
- No database changes or credentials are included.
