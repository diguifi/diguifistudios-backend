using Diguifi.Application.DTOs.Auth;
using Diguifi.Application.Interfaces;
using Diguifi.Domain.Entities;
using Diguifi.Infrastructure.Persistence;
using Diguifi.Infrastructure.Services;
using Diguifi.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Diguifi.Tests.Unit;

public sealed class AuthServiceTests
{
    // ── AuthenticateWithGoogleAsync ─────────────────────────────────────────

    [Fact]
    public async Task AuthenticateWithGoogleAsync_InvalidGoogleToken_ReturnsFailure()
    {
        await using var db = DbContextFactory.Create();
        var validator = new Mock<IGoogleTokenValidator>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((GoogleIdentity?)null);

        var sut = BuildService(db, validator.Object);
        var result = await sut.AuthenticateWithGoogleAsync(new GoogleAuthRequest { IdToken = "bad" }, null, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("invalid_google_token");
        db.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task AuthenticateWithGoogleAsync_NewUser_CreatesUserInDatabase()
    {
        await using var db = DbContextFactory.Create();
        var sut = BuildService(db, ValidValidator("sub-123", "new@example.com", "New User"));

        await sut.AuthenticateWithGoogleAsync(new GoogleAuthRequest { IdToken = "tok" }, null, null, CancellationToken.None);

        db.Users.Should().ContainSingle(u => u.Email == "new@example.com");
    }

    [Fact]
    public async Task AuthenticateWithGoogleAsync_NewUser_ReturnsSuccess()
    {
        await using var db = DbContextFactory.Create();
        var sut = BuildService(db, ValidValidator("sub-123", "new@example.com"));

        var result = await sut.AuthenticateWithGoogleAsync(new GoogleAuthRequest { IdToken = "tok" }, null, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AuthenticateWithGoogleAsync_ExistingUserByGoogleSubject_UpdatesAndDoesNotDuplicate()
    {
        await using var db = DbContextFactory.Create();
        db.Users.Add(new User { GoogleSubject = "sub-existing", Email = "old@example.com", Name = "Old" });
        await db.SaveChangesAsync();

        var sut = BuildService(db, ValidValidator("sub-existing", "updated@example.com", "Updated"));
        await sut.AuthenticateWithGoogleAsync(new GoogleAuthRequest { IdToken = "tok" }, null, null, CancellationToken.None);

        db.Users.Should().ContainSingle();
        db.Users.Single().Email.Should().Be("updated@example.com");
    }

    [Fact]
    public async Task AuthenticateWithGoogleAsync_ExistingUserByEmail_UpdatesAndDoesNotDuplicate()
    {
        await using var db = DbContextFactory.Create();
        db.Users.Add(new User { GoogleSubject = "old-sub", Email = "known@example.com", Name = "Known" });
        await db.SaveChangesAsync();

        var sut = BuildService(db, ValidValidator("new-sub", "known@example.com", "Known Updated"));
        await sut.AuthenticateWithGoogleAsync(new GoogleAuthRequest { IdToken = "tok" }, null, null, CancellationToken.None);

        db.Users.Should().ContainSingle();
        db.Users.Single().GoogleSubject.Should().Be("new-sub");
    }

    [Fact]
    public async Task AuthenticateWithGoogleAsync_SavesRefreshTokenWithIpAndAgent()
    {
        await using var db = DbContextFactory.Create();
        var sut = BuildService(db, ValidValidator("sub", "user@example.com"));

        await sut.AuthenticateWithGoogleAsync(new GoogleAuthRequest { IdToken = "tok" }, "1.2.3.4", "Mozilla/5.0", CancellationToken.None);

        db.RefreshTokens.Should().ContainSingle();
        db.RefreshTokens.Single().IpAddress.Should().Be("1.2.3.4");
    }

    [Fact]
    public async Task AuthenticateWithGoogleAsync_AdminEmail_IsAdminTrueInResponse()
    {
        await using var db = DbContextFactory.Create();
        var sut = BuildService(db, ValidValidator("sub", "lumia-diguifi@hotmail.com"));

        var result = await sut.AuthenticateWithGoogleAsync(new GoogleAuthRequest { IdToken = "tok" }, null, null, CancellationToken.None);

        result.Value!.User.IsAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task AuthenticateWithGoogleAsync_NonAdminEmail_IsAdminFalseInResponse()
    {
        await using var db = DbContextFactory.Create();
        var sut = BuildService(db, ValidValidator("sub", "regular@example.com"));

        var result = await sut.AuthenticateWithGoogleAsync(new GoogleAuthRequest { IdToken = "tok" }, null, null, CancellationToken.None);

        result.Value!.User.IsAdmin.Should().BeFalse();
    }

    // ── RefreshAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_EmptyToken_ReturnsFailure()
    {
        await using var db = DbContextFactory.Create();
        var result = await BuildService(db).RefreshAsync("", null, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("missing_refresh_token");
    }

    [Fact]
    public async Task RefreshAsync_WhitespaceToken_ReturnsFailure()
    {
        await using var db = DbContextFactory.Create();
        var result = await BuildService(db).RefreshAsync("   ", null, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("missing_refresh_token");
    }

    [Fact]
    public async Task RefreshAsync_TokenNotInDatabase_ReturnsFailure()
    {
        await using var db = DbContextFactory.Create();
        var result = await BuildService(db).RefreshAsync("no-such-token", null, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("invalid_refresh_token");
    }

    [Fact]
    public async Task RefreshAsync_RevokedToken_ReturnsFailure()
    {
        await using var db = DbContextFactory.Create();
        var tokenSvc = BuildTokenService();
        var rawToken = tokenSvc.GenerateRefreshToken();
        await SeedUserWithToken(db, tokenSvc, rawToken, revokedAt: DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = await BuildService(db, tokenService: tokenSvc).RefreshAsync(rawToken, null, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("invalid_refresh_token");
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ReturnsFailure()
    {
        await using var db = DbContextFactory.Create();
        var tokenSvc = BuildTokenService();
        var rawToken = tokenSvc.GenerateRefreshToken();
        await SeedUserWithToken(db, tokenSvc, rawToken, expiresAt: DateTimeOffset.UtcNow.AddDays(-1));

        var result = await BuildService(db, tokenService: tokenSvc).RefreshAsync(rawToken, null, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("invalid_refresh_token");
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_ReturnsNewAccessAndRefreshTokens()
    {
        await using var db = DbContextFactory.Create();
        var tokenSvc = BuildTokenService();
        var rawToken = tokenSvc.GenerateRefreshToken();
        await SeedUserWithToken(db, tokenSvc, rawToken);

        var result = await BuildService(db, tokenService: tokenSvc).RefreshAsync(rawToken, null, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBe(rawToken);
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_RevokesOldToken()
    {
        await using var db = DbContextFactory.Create();
        var tokenSvc = BuildTokenService();
        var rawToken = tokenSvc.GenerateRefreshToken();
        await SeedUserWithToken(db, tokenSvc, rawToken);

        await BuildService(db, tokenService: tokenSvc).RefreshAsync(rawToken, null, null, CancellationToken.None);

        db.RefreshTokens.OrderBy(t => t.CreatedAt).First().RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_AddsNewRefreshTokenToDatabase()
    {
        await using var db = DbContextFactory.Create();
        var tokenSvc = BuildTokenService();
        var rawToken = tokenSvc.GenerateRefreshToken();
        await SeedUserWithToken(db, tokenSvc, rawToken);

        await BuildService(db, tokenService: tokenSvc).RefreshAsync(rawToken, "10.0.0.1", null, CancellationToken.None);

        db.RefreshTokens.Should().HaveCount(2);
        db.RefreshTokens.OrderByDescending(t => t.CreatedAt).First().IpAddress.Should().Be("10.0.0.1");
    }

    // ── LogoutAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_EmptyToken_ReturnsSuccessWithoutChanges()
    {
        await using var db = DbContextFactory.Create();
        var result = await BuildService(db).LogoutAsync("", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.RefreshTokens.Should().BeEmpty();
    }

    [Fact]
    public async Task LogoutAsync_UnknownToken_ReturnsSuccess()
    {
        await using var db = DbContextFactory.Create();
        var result = await BuildService(db).LogoutAsync("ghost-token", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAsync_ValidToken_SetsRevokedAt()
    {
        await using var db = DbContextFactory.Create();
        var tokenSvc = BuildTokenService();
        var rawToken = tokenSvc.GenerateRefreshToken();
        await SeedUserWithToken(db, tokenSvc, rawToken);

        await BuildService(db).LogoutAsync(rawToken, CancellationToken.None);

        db.RefreshTokens.Single().RevokedAt.Should().NotBeNull();
    }

    // ── GetCurrentUserAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUserAsync_ExistingUser_ReturnsProfile()
    {
        await using var db = DbContextFactory.Create();
        var user = new User { Email = "me@example.com", Name = "Me", FirstName = "Me" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await BuildService(db).GetCurrentUserAsync(user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("me@example.com");
        result.Value.Id.Should().Be(user.Id.ToString());
    }

    [Fact]
    public async Task GetCurrentUserAsync_UnknownUser_ReturnsFailure()
    {
        await using var db = DbContextFactory.Create();
        var result = await BuildService(db).GetCurrentUserAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("user_not_found");
    }

    [Fact]
    public async Task GetCurrentUserAsync_AdminEmail_IsAdminTrue()
    {
        await using var db = DbContextFactory.Create();
        var user = new User { Email = "lumia-diguifi@hotmail.com", Name = "Admin", FirstName = "Admin" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await BuildService(db).GetCurrentUserAsync(user.Id, CancellationToken.None);

        result.Value!.IsAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithUnreadNotification_HasNotificationTrue()
    {
        await using var db = DbContextFactory.Create();
        var user = new User { Email = "notify@example.com", Name = "U", FirstName = "U" };
        db.Users.Add(user);
        db.Notifications.Add(new Notification { UserId = user.Id, Text = "Hello", IsRead = false });
        await db.SaveChangesAsync();

        var result = await BuildService(db).GetCurrentUserAsync(user.Id, CancellationToken.None);

        result.Value!.HasNotification.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentUserAsync_AllNotificationsRead_HasNotificationFalse()
    {
        await using var db = DbContextFactory.Create();
        var user = new User { Email = "read@example.com", Name = "U", FirstName = "U" };
        db.Users.Add(user);
        db.Notifications.Add(new Notification { UserId = user.Id, Text = "Done", IsRead = true });
        await db.SaveChangesAsync();

        var result = await BuildService(db).GetCurrentUserAsync(user.Id, CancellationToken.None);

        result.Value!.HasNotification.Should().BeFalse();
    }

    [Fact]
    public async Task GetCurrentUserAsync_NoNotifications_HasNotificationFalse()
    {
        await using var db = DbContextFactory.Create();
        var user = new User { Email = "none@example.com", Name = "U", FirstName = "U" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await BuildService(db).GetCurrentUserAsync(user.Id, CancellationToken.None);

        result.Value!.HasNotification.Should().BeFalse();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static AuthService BuildService(
        AppDbContext db,
        IGoogleTokenValidator? validator = null,
        JwtTokenService? tokenService = null)
    {
        var opts = JwtOptionsFactory.Create();
        return new AuthService(db, validator ?? new Mock<IGoogleTokenValidator>().Object, tokenService ?? new JwtTokenService(opts), opts);
    }

    private static JwtTokenService BuildTokenService() => new(JwtOptionsFactory.Create());

    private static IGoogleTokenValidator ValidValidator(string subject, string email, string name = "User")
    {
        var mock = new Mock<IGoogleTokenValidator>();
        mock.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleIdentity { Subject = subject, Email = email, Name = name, FirstName = name, AvatarUrl = "" });
        return mock.Object;
    }

    private static async Task SeedUserWithToken(
        AppDbContext db, JwtTokenService tokenSvc, string rawToken,
        DateTimeOffset? revokedAt = null, DateTimeOffset? expiresAt = null)
    {
        var user = new User { Email = "user@example.com", Name = "User", FirstName = "User" };
        db.Users.Add(user);
        db.RefreshTokens.Add(new RefreshToken
        {
            User = user,
            TokenHash = tokenSvc.ComputeHash(rawToken),
            ExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddDays(30),
            RevokedAt = revokedAt
        });
        await db.SaveChangesAsync();
    }
}
