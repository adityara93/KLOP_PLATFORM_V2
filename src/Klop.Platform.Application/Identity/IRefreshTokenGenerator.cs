namespace Klop.Platform.Application.Identity;

/// <summary>Generates opaque bearer credentials. Only their hashes belong in persistent storage.</summary>
public interface IRefreshTokenGenerator
{
    GeneratedRefreshToken Generate();
    byte[] Hash(string token);
}
public sealed record GeneratedRefreshToken(string Token, byte[] TokenHash);
