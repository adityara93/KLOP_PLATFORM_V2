using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Klop.Platform.IntegrationTests;

public sealed class LivenessTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public LivenessTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Liveness_ReturnsOk()
    {
        using var configured = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "klop-integration-tests",
                    ["Jwt:Audience"] = "klop-integration-tests",
                    ["Jwt:SigningKey"] = "integration-test-only-not-a-production-secret-123456",
                    ["Jwt:AccessTokenMinutes"] = "15"
                })));
        using var client = configured.CreateClient();
        var response = await client.GetAsync("/health/live");
        Assert.True(response.IsSuccessStatusCode);
    }
}
