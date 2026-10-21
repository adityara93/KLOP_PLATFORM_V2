# KLOP_PLATFORM_V2

New ASP.NET Core Clean Architecture foundation for KLOP Platform V2. V2-01 only; independent of KLOP Platform V1.

## Requirements
.NET 10 SDK.

## Commands
```bash
dotnet restore KLOP_PLATFORM_V2.sln
dotnet build KLOP_PLATFORM_V2.sln -c Release
dotnet test KLOP_PLATFORM_V2.sln -c Release
dotnet run --project src/Klop.Platform.Api
```

Liveness: GET /health/live. No database is required for V2-01. See docs/architecture.md.

## Verification
The GitHub Actions workflow restores, builds and tests on every PR to main. Do not merge a PR unless the CI checks are green.
