# V2-03 Phase B — Password Hashing

Implements a versioned PBKDF2-SHA256 password hasher in Infrastructure and an application contract.

- 600,000 iterations; 16-byte cryptographically random salt; 32-byte derived key.
- Constant-time comparison; malformed hashes rejected; iteration count bounded.
- Rehash signal for older iterations; no plaintext passwords stored.
- No database changes, no endpoints, no secrets or login activation in this phase.

Pending: repository persistence, JWT, refresh rotation, session revocation, rate limiting, audit and integration tests.
