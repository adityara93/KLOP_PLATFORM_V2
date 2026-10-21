namespace Klop.Platform.Application.Identity;

/// <summary>Persistence boundary. Implementations must use atomic transactions and prevent refresh replay.</summary>
public interface IUserSessionStore
{
    Task CreateAsync(NewUserSession session, CancellationToken cancellationToken = default);
    Task<RefreshRotationResult> RotateAsync(byte[] presentedTokenHash, byte[] replacementTokenHash, DateTimeOffset replacementExpiry, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken = default);
}

public sealed record NewUserSession(Guid SessionId, long UserId, DateTimeOffset SessionExpiresAt, Guid TokenFamilyId, byte[] RefreshTokenHash, DateTimeOffset RefreshTokenExpiresAt, string? IpAddress, string? UserAgent);
public enum RefreshRotationStatus { Success, Invalid, Replayed }
public sealed record RefreshRotationResult(RefreshRotationStatus Status, Guid? SessionId, long? UserId);
