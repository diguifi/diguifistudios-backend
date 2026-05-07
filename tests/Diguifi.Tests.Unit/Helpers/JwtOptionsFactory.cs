using Diguifi.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Diguifi.Tests.Unit.Helpers;

internal static class JwtOptionsFactory
{
    internal static IOptions<JwtOptions> Create(int accessTokenMinutes = 30, int refreshTokenDays = 30) =>
        Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = "test-signing-key-must-be-long-enough-32",
            AccessTokenMinutes = accessTokenMinutes,
            RefreshTokenDays = refreshTokenDays
        });
}
