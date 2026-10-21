namespace Klop.Platform.Application.Identity;

/// <summary>Reads an authentication candidate; callers must never expose whether a username exists.</summary>
public interface IIdentityCredentialReader
{
    Task<AuthenticationCandidate?> FindAsync(string normalizedUsername, string tenantCode, CancellationToken cancellationToken = default);
}

public sealed record AuthenticationCandidate(long UserId, int? TenantId, string Scope, string PasswordHash, bool IsUserActive, bool IsTenantActive, int FailedLoginCount, DateTimeOffset? LockoutEndAt);
