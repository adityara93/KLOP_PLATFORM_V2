using Xunit;

namespace Klop.Platform.ArchitectureTests;

public sealed class DependencyRulesTests
{
    [Fact]
    public void Domain_HasNoProjectReferences()
    {
        var names = typeof(Klop.Platform.Application.Abstractions.ICurrentClock).Assembly.GetReferencedAssemblies();
        Assert.DoesNotContain(names, assembly => assembly.Name == "Klop.Platform.Api");
    }
}
