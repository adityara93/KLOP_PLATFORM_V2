# KLOP Platform V2 — Architecture baseline

Clean Architecture with modular monolith deployment:

- Domain has no project dependencies.
- Application references Domain only.
- Infrastructure references Application and Domain, and implements persistence/integrations.
- API references Application and Infrastructure to compose dependencies.
- No business application, database schema, JWT, SSO or tenant implementation in V2-01.
- EF Core and stored procedures are planned for V2-02; they must share transactions when mixed.
- Every TENANT user is bound to exactly one tenant. Only PLATFORM admin can cross tenant boundaries, with audit.
- Module access is tenant-scoped; internal business permissions are owned by business applications.

## API policies
- Global exception middleware uses ASP.NET Core ProblemDetails.
- Structured ILogger logging and correlation ID.
- /health/live is dependency-free; readiness to be introduced after dependencies exist.
- OpenAPI is exposed in development only.
