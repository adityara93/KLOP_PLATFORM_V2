namespace Klop.Platform.Application.Identity;
public interface IPasswordHasher { string Hash(string password); PasswordVerificationResult Verify(string storedHash, string password); }
public enum PasswordVerificationResult { Failed, Success, SuccessRehashNeeded }
