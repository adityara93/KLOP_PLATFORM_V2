using Klop.Platform.Domain.Identity;

namespace Klop.Platform.Application.Identity;

/// <summary>Internal authentication workflow. Do not expose over HTTP until concurrency and audit gates pass.</summary>
public sealed class AuthenticationOrchestrator(
    IIdentityCredentialReader identities,
    IPasswordHasher passwords,
    ILoginSecurityStore security,
    IUserSessionStore sessions,
    IRefreshTokenGenerator refreshTokens,
    IAccessTokenIssuer accessTokens,
    TimeProvider clock)
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(7);

    public async Task<AuthenticationResult?> AuthenticateAsync(string? username, string? password,
        string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        if (!LoginIdentity.TryParse(username, out var login) || login is null || string.IsNullOrEmpty(password)) return null;
        var candidate = await identities.FindAsync(login.NormalizedUsername, login.TenantCode, cancellationToken);
        if (candidate is null || !candidate.IsUserActive || !candidate.IsTenantActive ||
            candidate.Scope != (login.IsPlatform ? "PLATFORM" : "TENANT") ||
            (login.IsPlatform ? candidate.TenantId is not null : candidate.TenantId is null or <= 0)) return null;
        var now = clock.GetUtcNow();
        if (candidate.LockoutEndAt is { } locked && locked > now) return null;
        if (passwords.Verify(candidate.PasswordHash, password) == PasswordVerificationResult.Failed)
        {
            await security.RecordFailureAsync(candidate.UserId, now, cancellationToken);
            return null;
        }
        // Phase I does not issue tokens to an account whose failed-attempt counter is already at threshold.
        if (candidate.FailedLoginCount >= 5 && candidate.LockoutEndAt is null) return null;
        var sessionId = Guid.NewGuid();
        var generated = refreshTokens.Generate();
        var expiry = now.Add(SessionLifetime);
        await sessions.CreateAsync(new NewUserSession(sessionId, candidate.UserId, expiry,
            Guid.NewGuid(), generated.TokenHash, expiry, ipAddress, userAgent), cancellationToken);
        try
        {
            await security.RecordSuccessAsync(candidate.UserId, candidate.TenantId, now, cancellationToken: cancellationToken);
        }
        catch
        {
            await sessions.RevokeAsync(sessionId, now, CancellationToken.None);
            throw;
        }
        var access = accessTokens.Issue(new AccessTokenPrincipal(candidate.UserId, candidate.TenantId, candidate.Scope, sessionId));
        return new AuthenticationResult(access.Token, access.ExpiresAt, generated.Token, expiry, sessionId);
    }
}

public sealed record AuthenticationResult(string AccessToken, DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken, DateTimeOffset RefreshTokenExpiresAt, Guid SessionId);
