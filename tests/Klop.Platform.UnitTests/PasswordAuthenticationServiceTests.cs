using Klop.Platform.Application.Identity;
using Xunit;
namespace Klop.Platform.UnitTests;
public sealed class PasswordAuthenticationServiceTests
{
    private sealed class Reader(AuthenticationCandidate? candidate) : IIdentityCredentialReader
    {
        public Task<AuthenticationCandidate?> FindAsync(string username, string tenant, CancellationToken ct = default) => Task.FromResult(candidate);
    }
    private sealed class Hasher : IPasswordHasher
    {
        public string Hash(string password) => "hash";
        public PasswordVerificationResult Verify(string hash, string password) => password == "correct" ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
    }
    private static PasswordAuthenticationService Service(AuthenticationCandidate? candidate) => new(new Reader(candidate), new Hasher(), TimeProvider.System);
    [Fact] public async Task ValidTenantCredentialsPass()
    {
        var result = await Service(new(42, 4, "TENANT", "hash", true, true, 0, null)).VerifyAsync("user@100002", "correct");
        Assert.NotNull(result);
        Assert.Equal(4, result.TenantId);
    }
    [Fact] public async Task WrongPasswordFails() => Assert.Null(await Service(new(42, 4, "TENANT", "hash", true, true, 0, null)).VerifyAsync("user@100002", "wrong"));
    [Fact] public async Task InactiveTenantFails() => Assert.Null(await Service(new(42, 4, "TENANT", "hash", true, false, 0, null)).VerifyAsync("user@100002", "correct"));
    [Fact] public async Task LockedAccountFails() => Assert.Null(await Service(new(42, 4, "TENANT", "hash", true, true, 6, DateTimeOffset.UtcNow.AddMinutes(10))).VerifyAsync("user@100002", "correct"));
    [Fact] public async Task CrossScopeFails() => Assert.Null(await Service(new(42, 4, "TENANT", "hash", true, true, 0, null)).VerifyAsync("admin@PLATFORM", "correct"));
    [Fact] public async Task UnknownUserFails() => Assert.Null(await Service(null).VerifyAsync("user@100002", "correct"));
}
