namespace Klop.Platform.Infrastructure.Persistence.Migrations;

internal static class SqlScript
{
    // SQL is embedded at build time as a resource and loaded by the migration.
    internal static string Value
    {
        get
        {
            using var stream = typeof(SqlScript).Assembly.GetManifestResourceStream("Klop.Platform.Infrastructure.Persistence.Migrations.V2-02-schema.sql")
                ?? throw new InvalidOperationException("Embedded V2-02 SQL schema was not found.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
