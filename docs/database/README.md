# V2-02 SQL Server schema

- `V2-02-schema.sql` is the reviewed **reference DDL** for an empty `KLOP_PLATFORM_V2_DB`. Do not run it on V1 or on a database already managed by EF migrations.
- EF Core migrations are the deployment mechanism. Reference SQL and migration represent the same 16-table baseline; **choose one bootstrap path**, never execute both.
- No seed passwords, credentials, or production changes.
- SQL Server collation should be configured case-insensitive for TenantCode and normalized usernames. App must normalize using invariant uppercase and reject ambiguous names.
- `PLATFORM` is reserved; PLATFORM admin has NULL TenantId and username `admin`.
- Unique filtered index ensures at most one PLATFORM admin; provisioning must ensure one exists.
- Audit is append-only by application policy; restrict SQL UPDATE/DELETE grants in production.
- A user-scope/role-scope composite FK prevents mixed role scopes. Tenant isolation must also be enforced in application authorization and all queries.
- Token hashes are SHA-256 binary digests, never plaintext; secrets must not be logged.
- ApplicationSetting and PlatformSetting support SecretRef instead of storing secret values.
- The launch handshake/SSO contract and deeper security policy are deferred to subsequent workstreams.
