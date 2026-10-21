using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Klop.Platform.Infrastructure.Persistence;

public sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("KLOP_V2_SQL_CONNECTION")
            ?? throw new InvalidOperationException("Set KLOP_V2_SQL_CONNECTION before using EF Core tools.");
        return new PlatformDbContext(new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlServer(connectionString).Options);
    }
}
