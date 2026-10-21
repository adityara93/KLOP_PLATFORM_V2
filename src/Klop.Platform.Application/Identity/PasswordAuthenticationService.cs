using Klop.Platform.Domain.Identity;

namespace Klop.Platform.Application.Identity;

/// <summary>Verifies credentials without disclosing user existence or account state to callers.</summary>
public sealed class PasswordAuthenticationService(IIdentityCredentialReader reader, IPasswordHasher hasher, TimeProvider clock)
{
    public async Task<AuthenticatedIdentity?> VerifyAsync(string? username, string? password, CancellationToken cancellationToken = default)
    {
        if (!LoginIdentity.TryParse(username, out var login) || login is null || string.IsNullOrEmpty(password)) return null;
        var candidate = await reader.FindAsync(login.NormalizedUsername, login.TenantCode, cancellationToken);
        if (candidate is null || !candidate.IsUserActive || !candidate.IsTenantActive ||
            (candidate.LockoutEndAt is { } until && until > clock.GetUtcNow()) ||
            candidate.Scope != (login.IsPlatform ? "PLATFORM" : "TENANT") ||
            (login.IsPlatform ? candidate.TenantId is not null : candidate.TenantId is null or <= 0)) return null;
        var result = hasher.Verify(candidate.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return null;
        return new AuthenticatedIdentity(candidate.UserId, candidate.TenantId, candidate.Scope, result == PasswordVerificationResult.SuccessRehashNeeded);
    }
}

public sealed record AuthenticatedIdentity(long UserId, int? TenantId, string Scope, bool NeedsPasswordRehash);
