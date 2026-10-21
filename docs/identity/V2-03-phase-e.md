# V2-03 Phase E — Password Authentication Service

This phase introduces the application-level credential verification orchestration: canonical login syntax, read-only identity lookup, active user/tenant and lockout checks, scope/tenant consistency, and password verification.

**Not production login yet.** Failed-login increments, atomic lockout enforcement, constant-response timing mitigation, rate limits, password rehash persistence, audit logging, session creation, refresh token rotation, JWT validation and HTTP endpoints remain outstanding. Do not expose this service directly over HTTP until those are completed.

The existing reader supplies FailedLoginCount but Phase E only checks LockoutEndAt. The count is not yet incremented or enforced; a later transactional security service must implement that behavior.
