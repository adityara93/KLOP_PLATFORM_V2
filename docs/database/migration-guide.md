# EF Core Migration Guide — V2-02

## Safety
Do not run against the V1 database. Use a new empty SQL Server database named `KLOP_PLATFORM_V2_DB`. Back up before applying to an existing environment. Review SQL first.

## Tools
Install .NET 10 SDK and matching `dotnet-ef` 10.x. Configure `KLOP_V2_SQL_CONNECTION` securely (never commit it).

## Apply
```bash
dotnet restore KLOP_PLATFORM_V2.sln
dotnet build KLOP_PLATFORM_V2.sln -c Release
dotnet ef database update --project src/Klop.Platform.Infrastructure --startup-project src/Klop.Platform.Infrastructure
```

The migration embeds the reference SQL DDL. Do not execute `docs/database/V2-02-schema.sql` manually on the same database before EF migration. Migration rollback is intentionally blocked because dropping the schema destroys data.

**Status:** This migration is submitted for review; it has not been applied to any database.
