using System.IdentityModel.Tokens.Jwt;
using Diguifi.Domain.Entities;
using Diguifi.Infrastructure.Services;
using Diguifi.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Xunit;

namespace Diguifi.Tests.Unit;

public sealed class JwtTokenServiceTests
{
    private readonly JwtTokenService _sut;

    public JwtTokenServiceTests()
    {
        _sut = new JwtTokenService(JwtOptionsFactory.Create());
    }

    [Fact]
    public void CreateAccessToken_ReturnsNonEmptyToken()
    {
        var user = BuildUser("user@example.com");
        var (token, expiresAt) = _sut.CreateAccessToken(user);

        token.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void CreateAccessToken_ContainsSubjectClaim()
    {
        var user = BuildUser("user@example.com");
        var (token, _) = _sut.CreateAccessToken(user);

        var claims = ParseClaims(token);
        claims.Should().ContainKey("sub").WhoseValue.Should().Be(user.Id.ToString());
    }

    [Fact]
    public void CreateAccessToken_NonAdminEmail_DoesNotContainAdminRole()
    {
        var user = BuildUser("regular@example.com");
        var (token, _) = _sut.CreateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        jwt.Claims.Should().NotContain(c => c.Type == "role" && c.Value == "admin");
    }

    [Theory]
    [InlineData("lumia-diguifi@hotmail.com")]
    [InlineData("diego.penha95@gmail.com")]
    public void CreateAccessToken_AdminEmail_ContainsAdminRoleClaim(string email)
    {
        var user = BuildUser(email);
        var (token, _) = _sut.CreateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        jwt.Claims.Should().Contain(c =>
            c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" &&
            c.Value == "admin");
    }

    [Fact]
    public void CreateAccessToken_ExpiresAfterConfiguredMinutes()
    {
        var sut = new JwtTokenService(JwtOptionsFactory.Create(accessTokenMinutes: 15));
        var user = BuildUser("user@example.com");
        var before = DateTimeOffset.UtcNow;

        var (_, expiresAt) = sut.CreateAccessToken(user);

        expiresAt.Should().BeCloseTo(before.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        var token = _sut.GenerateRefreshToken();
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsDifferentValuesEachCall()
    {
        var t1 = _sut.GenerateRefreshToken();
        var t2 = _sut.GenerateRefreshToken();
        t1.Should().NotBe(t2);
    }

    [Fact]
    public void ComputeHash_SameInput_ReturnsSameHash()
    {
        var h1 = _sut.ComputeHash("my-token");
        var h2 = _sut.ComputeHash("my-token");
        h1.Should().Be(h2);
    }

    [Fact]
    public void ComputeHash_DifferentInputs_ReturnsDifferentHashes()
    {
        var h1 = _sut.ComputeHash("token-a");
        var h2 = _sut.ComputeHash("token-b");
        h1.Should().NotBe(h2);
    }

    [Fact]
    public void ComputeHash_ReturnsHexString()
    {
        var hash = _sut.ComputeHash("test");
        hash.Should().MatchRegex("^[0-9A-F]+$");
    }

    private static User BuildUser(string email) => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        Name = "Test User",
        FirstName = "Test"
    };

    private static Dictionary<string, string> ParseClaims(string token)
    {
        var options = JwtOptionsFactory.Create();
        var handler = new JwtSecurityTokenHandler();
        handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Value.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Value.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        }, out var validated);

        return ((JwtSecurityToken)validated).Claims.ToDictionary(c => c.Type, c => c.Value);
    }
}
