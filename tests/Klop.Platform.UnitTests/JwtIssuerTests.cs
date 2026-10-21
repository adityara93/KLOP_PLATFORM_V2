using System.IdentityModel.Tokens.Jwt;
using Klop.Platform.Application.Identity;
using Klop.Platform.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Xunit;
namespace Klop.Platform.UnitTests;
public sealed class JwtIssuerTests
{
    private static HmacAccessTokenIssuer Create() => new(Options.Create(new JwtOptions {
        Issuer = "klop-platform-v2-test", Audience = "klop-platform-v2-test-client",
        SigningKey = new string('X', 48), AccessTokenMinutes = 15
    }), TimeProvider.System);
    [Fact] public void TenantTokenHasTenantAndSessionClaims()
    {
        var sid = Guid.NewGuid();
        var result = Create().Issue(new AccessTokenPrincipal(42, 4, "TENANT", sid));
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal("42", token.Subject);
        Assert.Equal("4", token.Claims.Single(c => c.Type == "tenant_id").Value);
        Assert.Equal(sid.ToString("D"), token.Claims.Single(c => c.Type == "sid").Value);
    }
    [Fact] public void PlatformTokenDoesNotContainTenantClaim()
    {
        var result = Create().Issue(new AccessTokenPrincipal(1, null, "PLATFORM", Guid.NewGuid()));
        Assert.DoesNotContain(new JwtSecurityTokenHandler().ReadJwtToken(result.Token).Claims, c => c.Type == "tenant_id");
    }
    [Fact] public void RejectsScopeTenantMismatch()
    {
        Assert.Throws<ArgumentException>(() => Create().Issue(new AccessTokenPrincipal(1, 4, "PLATFORM", Guid.NewGuid())));
        Assert.Throws<ArgumentException>(() => Create().Issue(new AccessTokenPrincipal(1, null, "TENANT", Guid.NewGuid())));
    }
    [Fact] public void RejectsWeakSecret()
    {
        Assert.Throws<InvalidOperationException>(() => new HmacAccessTokenIssuer(Options.Create(new JwtOptions {
            Issuer = "issuer", Audience = "audience", SigningKey = "weak"
        }), TimeProvider.System));
    }
}
