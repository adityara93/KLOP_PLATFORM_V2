using Microsoft.EntityFrameworkCore;

namespace Klop.Platform.Infrastructure.Persistence;

/// <summary>V2 schema mapping; business entity behavior is introduced incrementally.</summary>
public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);
    }
}
