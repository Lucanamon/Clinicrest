using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using api.Domain.Entities;
using api.Infrastructure.Auth;
using api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    ApplicationDbContext dbContext,
    IConfiguration configuration,
    IPasswordHasher<User> passwordHasher) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == request.Username.Trim());

        if (user is null || !AuthPasswordValidation.VerifyPassword(passwordHasher, user, request.Password))
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var token = GenerateJwt(user.Id.ToString(), user.Username, user.Role.ToString());
        return Ok(new LoginResponse(token, user.Username, user.Role.ToString()));
    }

    private string GenerateJwt(string userId, string username, string role)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"] ?? "clinicrest-api";
        var audience = jwtSection["Audience"] ?? "clinicrest-web";
        var secret = jwtSection["Secret"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("Jwt:Secret is not configured.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        var expires = DateTime.UtcNow.AddMinutes(60);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, string Role);
