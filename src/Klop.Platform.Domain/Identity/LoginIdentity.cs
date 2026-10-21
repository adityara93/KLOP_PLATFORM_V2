namespace Klop.Platform.Domain.Identity;

/// <summary>Canonical, tenant-scoped login identity. Never derive authorization from the supplied suffix alone.</summary>
public sealed record LoginIdentity(string Username, string NormalizedUsername, string TenantCode, bool IsPlatform)
{
    public const string ReservedPlatformCode = "PLATFORM";

    public static bool TryParse(string? login, out LoginIdentity? identity)
    {
        identity = null;
        if (string.IsNullOrWhiteSpace(login) || login.Length > 131 || login != login.Trim()) return false;
        var separator = login.IndexOf('@');
        if (separator <= 0 || separator != login.LastIndexOf('@') || separator >= login.Length - 1) return false;
        var username = login[..separator];
        var tenantCode = login[(separator + 1)..];
        if (username.Length > 100 || tenantCode.Length > 30 ||
            username.Any(char.IsControl) || username.Any(char.IsWhiteSpace) ||
            !username.All(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-') ||
            !tenantCode.All(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-')) return false;
        var normalizedCode = tenantCode.ToUpperInvariant();
        var isPlatform = normalizedCode == ReservedPlatformCode;
        if (isPlatform && !string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase)) return false;
        identity = new LoginIdentity(username, username.ToUpperInvariant(), normalizedCode, isPlatform);
        return true;
    }
}
