using Diguifi.Application.DTOs.Auth;
using Diguifi.Application.Interfaces;
using Diguifi.Infrastructure.Options;
using Diguifi.Infrastructure.Persistence;
using Diguifi.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Diguifi.Tests.Unit;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task AuthenticateWithGoogleAsync_ShouldCreateUserAndReturnToken()
    {
        await using var dbContext = CreateDbContext();
        var googleValidator = new Mock<IGoogleTokenValidator>();
        googleValidator
            .Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleIdentity
            {
                Subject = "google-subject",
                Email = "user@example.com",
                Name = "User Example",
                FirstName = "User",
                AvatarUrl = "https://example.com/avatar.png"
            });

        var tokenService = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            SigningKey = "01234567890123456789012345678901"
        }));

        var service = new AuthService(dbContext, googleValidator.Object, tokenService, Options.Create(new JwtOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            SigningKey = "01234567890123456789012345678901"
        }));

        var result = await service.AuthenticateWithGoogleAsync(new GoogleAuthRequest
        {
            IdToken = "token",
            Credential = "token"
        }, null, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        dbContext.Users.Should().ContainSingle();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
