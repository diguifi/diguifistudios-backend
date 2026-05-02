using Diguifi.Domain.Entities;

namespace Diguifi.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user);
    string GenerateRefreshToken();
    string ComputeHash(string value);
}
