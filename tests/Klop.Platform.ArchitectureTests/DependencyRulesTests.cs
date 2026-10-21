using Klop.Platform.Application.Abstractions;
using Xunit;

namespace Klop.Platform.ArchitectureTests;

public sealed class DependencyRulesTests
{
    [Fact]
    public void Domain_DoesNotDependOnOuterLayers()
    {
        var references = typeof(Klop.Platform.Domain.DomainAssemblyMarker).Assembly.GetReferencedAssemblies();
        Assert.DoesNotContain(references, x => x.Name is "Klop.Platform.Application" or "Klop.Platform.Infrastructure" or "Klop.Platform.Api");
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi()
    {
        var references = typeof(ICurrentClock).Assembly.GetReferencedAssemblies();
        Assert.DoesNotContain(references, x => x.Name is "Klop.Platform.Infrastructure" or "Klop.Platform.Api");
    }
}
