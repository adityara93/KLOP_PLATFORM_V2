using Klop.Platform.Infrastructure.Identity;
using Xunit;
namespace Klop.Platform.UnitTests;
public sealed class RefreshTokenGeneratorTests
{
    [Fact] public void GeneratesUniqueOpaqueTokensWithSha256Hashes()
    {
        var generator = new SecureRefreshTokenGenerator();
        var a = generator.Generate();
        var b = generator.Generate();
        Assert.NotEqual(a.Token, b.Token);
        Assert.Equal(43, a.Token.Length);
        Assert.Equal(32, a.TokenHash.Length);
        Assert.Equal(generator.Hash(a.Token), a.TokenHash);
        Assert.NotEqual(a.Token, Convert.ToHexString(a.TokenHash));
    }
    [Fact] public void RejectsBlankToken()
    {
        Assert.Throws<ArgumentException>(() => new SecureRefreshTokenGenerator().Hash(""));
    }
}
