using Klop.Platform.Domain.Identity;
using Xunit;

namespace Klop.Platform.UnitTests;

public sealed class LoginIdentityTests
{
    [Theory]
    [InlineData("operator@100002", "OPERATOR", "100002", false)]
    [InlineData("admin@PLATFORM", "ADMIN", "PLATFORM", true)]
    [InlineData("Admin@platform", "ADMIN", "PLATFORM", true)]
    [InlineData("user.name@abc-01", "USER.NAME", "ABC-01", false)]
    public void ParsesValidIdentities(string value, string user, string tenant, bool platform)
    {
        Assert.True(LoginIdentity.TryParse(value, out var identity));
        Assert.NotNull(identity);
        Assert.Equal(user, identity.NormalizedUsername);
        Assert.Equal(tenant, identity.TenantCode);
        Assert.Equal(platform, identity.IsPlatform);
    }

    [Theory]
    [InlineData("")]
    [InlineData("admin")]
    [InlineData("admin@@PLATFORM")]
    [InlineData("operator@")]
    [InlineData("@100002")]
    [InlineData("operator@PLATFORM")]
    [InlineData(" admin@PLATFORM")]
    [InlineData("name with space@100002")]
    [InlineData("operator@tenant_name")]
    public void RejectsInvalidIdentities(string value)
    {
        Assert.False(LoginIdentity.TryParse(value, out var identity));
        Assert.Null(identity);
    }
}
