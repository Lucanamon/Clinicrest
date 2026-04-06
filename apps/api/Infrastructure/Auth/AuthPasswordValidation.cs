using api.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace api.Infrastructure.Auth;

public static class AuthPasswordValidation
{
    public static bool VerifyPassword(IPasswordHasher<User> passwordHasher, User user, string plainPassword)
    {
        if (IsBcryptHash(user.PasswordHash) &&
            BCrypt.Net.BCrypt.Verify(plainPassword, user.PasswordHash))
        {
            return true;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, plainPassword);
        if (result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded)
        {
            return true;
        }

        return LegacyPasswordHasher.Verify(plainPassword, user.PasswordHash);
    }

    private static bool IsBcryptHash(string hash)
    {
        if (string.IsNullOrEmpty(hash) || hash.Length < 4)
        {
            return false;
        }

        return hash.StartsWith("$2a$", StringComparison.Ordinal) ||
               hash.StartsWith("$2b$", StringComparison.Ordinal) ||
               hash.StartsWith("$2y$", StringComparison.Ordinal);
    }
}
