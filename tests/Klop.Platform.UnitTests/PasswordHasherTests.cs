using Klop.Platform.Application.Identity;
using Klop.Platform.Infrastructure.Identity;
using Xunit;
namespace Klop.Platform.UnitTests;
public sealed class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();
    [Fact] public void HashAndVerifySucceeds()
    {
        var hash = _hasher.Hash("Valid-Test-Password!2026");
        Assert.Equal(PasswordVerificationResult.Success, _hasher.Verify(hash, "Valid-Test-Password!2026"));
        Assert.DoesNotContain("Valid-Test-Password!2026", hash);
    }
    [Fact] public void SaltsDiffer() => Assert.NotEqual(_hasher.Hash("same-password"), _hasher.Hash("same-password"));
    [Fact] public void WrongPasswordFails()
    {
        var hash = _hasher.Hash("correct-password");
        Assert.Equal(PasswordVerificationResult.Failed, _hasher.Verify(hash, "wrong-password"));
    }
    [Theory]
    [InlineData("not-a-hash")]
    [InlineData("PBKDF2-SHA256-V1$999999999$AA==$AA==")]
    [InlineData("PBKDF2-SHA256-V1$600000$invalid$invalid")]
    public void MalformedHashFails(string hash) => Assert.Equal(PasswordVerificationResult.Failed, _hasher.Verify(hash, "password"));
}
