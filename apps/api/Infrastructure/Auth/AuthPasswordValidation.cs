using api.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace api.Infrastructure.Auth;

public static class AuthPasswordValidation
{
    public static bool VerifyPassword(IPasswordHasher<User> passwordHasher, User user, string plainPassword)
    {
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, plainPassword);
        if (result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded)
        {
            return true;
        }

        return LegacyPasswordHasher.Verify(plainPassword, user.PasswordHash);
    }
}
