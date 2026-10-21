using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Klop.Platform.IntegrationTests;

public sealed class LivenessTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public LivenessTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Liveness_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        Assert.True(response.IsSuccessStatusCode);
    }
}
