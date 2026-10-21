using System.Security.Cryptography;
using Klop.Platform.Application.Identity;
namespace Klop.Platform.Infrastructure.Identity;
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 600_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const string Version = "PBKDF2-SHA256-V1";
    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        if (password.Length is < 1 or > 1024) throw new ArgumentOutOfRangeException(nameof(password));
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return string.Join('$', Version, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(key));
    }
    public PasswordVerificationResult Verify(string storedHash, string password)
    {
        if (string.IsNullOrEmpty(storedHash) || password is null || password.Length is < 1 or > 1024) return PasswordVerificationResult.Failed;
        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != Version || !int.TryParse(parts[1], out var iterations) || iterations is < 100_000 or > 2_000_000) return PasswordVerificationResult.Failed;
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            if (salt.Length != SaltSize || expected.Length != KeySize) return PasswordVerificationResult.Failed;
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, KeySize);
            if (!CryptographicOperations.FixedTimeEquals(actual, expected)) return PasswordVerificationResult.Failed;
            return iterations < Iterations ? PasswordVerificationResult.SuccessRehashNeeded : PasswordVerificationResult.Success;
        }
        catch (FormatException) { return PasswordVerificationResult.Failed; }
    }
}
