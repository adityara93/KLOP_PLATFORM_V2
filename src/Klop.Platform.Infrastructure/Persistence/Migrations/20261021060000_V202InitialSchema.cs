using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Klop.Platform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("20261021060000_V202InitialSchema")]
public sealed class V202InitialSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Baseline schema is maintained as a single reviewed DDL artifact.
        // EF Core migration executes the same DDL inside the migration transaction.
        var ddl = SqlScript.Value
            .Replace("SET XACT_ABORT ON;\\nBEGIN TRANSACTION;\\n", string.Empty, StringComparison.Ordinal)
            .Replace("COMMIT TRANSACTION;", string.Empty, StringComparison.Ordinal);
        migrationBuilder.Sql(ddl);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Destructive downgrade is intentionally prohibited. Restore from backup.");
    }
}
