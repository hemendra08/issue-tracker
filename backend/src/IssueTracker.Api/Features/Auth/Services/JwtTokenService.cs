using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IssueTracker.Api.Features.Auth.Models;
using Microsoft.IdentityModel.Tokens;

namespace IssueTracker.Api.Features.Auth.Services;

public interface IJwtTokenService
{
    (string Token, int ExpiresIn) GenerateToken(User user);
}

/// <summary>
/// Service responsible for generating signed JWT access tokens with standard claims.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly byte[] _key;
    private readonly int _expirationMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        _issuer = jwtSection["Issuer"] ?? "IssueTracker.Api";
        _audience = jwtSection["Audience"] ?? "IssueTracker.Client";
        _expirationMinutes = int.TryParse(jwtSection["ExpirationMinutes"], out var exp) ? exp : 60;

        var signingKey = configuration["Jwt:SigningKey"] 
                      ?? Environment.GetEnvironmentVariable("JWT_SIGNING_KEY");

        if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey is missing or too short. It must be at least 32 characters long.");
        }

        _key = Encoding.UTF8.GetBytes(signingKey);
    }

    public (string Token, int ExpiresIn) GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var expires = DateTime.UtcNow.AddMinutes(_expirationMinutes);
        var expiresInSeconds = (int)(expires - DateTime.UtcNow).TotalSeconds;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("D"))
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_key),
                SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), expiresInSeconds);
    }
}
