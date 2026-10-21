using System.Security.Cryptography;
using Klop.Platform.Application.Identity;

namespace Klop.Platform.Infrastructure.Identity;

public sealed class SecureRefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenBytes = 32;
    public GeneratedRefreshToken Generate()
    {
        var token = Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));
        return new GeneratedRefreshToken(token, Hash(token));
    }
    public byte[] Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        if (token.Length > 256) throw new ArgumentOutOfRangeException(nameof(token));
        return SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(token));
    }
    private static string Base64UrlEncode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
