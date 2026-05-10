using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HabiHamAIAPI.Models;
using Microsoft.IdentityModel.Tokens;

namespace HabiHamAIAPI.Services;

public sealed class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(string username, AppUserRole role)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = GetRequiredSetting("JWT_KEY", jwtSection["Key"], "Jwt:Key");
        var issuer = GetRequiredSetting("JWT_ISSUER", jwtSection["Issuer"], "Jwt:Issuer");
        var audience = GetRequiredSetting("JWT_AUDIENCE", jwtSection["Audience"], "Jwt:Audience");
        var expiresMinutes = int.TryParse(jwtSection["ExpiresMinutes"], out var value) ? value : 60;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GetRequiredSetting(string envName, string? fallbackValue, string settingName)
    {
        var value = Environment.GetEnvironmentVariable(envName);
        if (string.IsNullOrWhiteSpace(value))
        {
            value = fallbackValue;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{settingName} is not configured.");
        }

        return value;
    }
}
