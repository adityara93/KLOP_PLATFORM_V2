namespace Klop.Platform.Application.Identity;

/// <summary>Persistence boundary for lockout and authentication audit. Never log passwords or tokens.</summary>
public interface ILoginSecurityStore
{
    Task<LoginFailureResult> RecordFailureAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task RecordSuccessAsync(long userId, int? tenantId, DateTimeOffset now, Guid? correlationId = null, CancellationToken cancellationToken = default);
}
public sealed record LoginFailureResult(int FailedAttempts, DateTimeOffset? LockedUntil);
