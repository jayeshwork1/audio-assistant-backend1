namespace AudioAssistant.Api.Utilities;

/// <summary>
/// Utility class for hashing and verifying passwords using BCrypt
/// </summary>
public class PasswordHasher
{
    /// <summary>
    /// Hashes a password using BCrypt
    /// </summary>
    /// <param name="password">The plain text password to hash</param>
    /// <returns>The hashed password</returns>
    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    /// <summary>
    /// Verifies a password against a hash
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="hash">The hash to verify against</param>
    /// <returns>True if the password matches the hash, false otherwise</returns>
    public static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
