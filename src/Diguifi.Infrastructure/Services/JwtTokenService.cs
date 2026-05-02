using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Diguifi.Application.Interfaces;
using Diguifi.Domain.Entities;
using Diguifi.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Diguifi.Infrastructure.Services;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("name", user.Name),
            new("first_name", user.FirstName)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string ComputeHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
