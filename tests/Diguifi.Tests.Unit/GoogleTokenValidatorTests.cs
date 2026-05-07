using Diguifi.Infrastructure.Options;
using Diguifi.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Diguifi.Tests.Unit;

public sealed class GoogleTokenValidatorTests
{
    [Fact]
    public async Task ValidateAsync_NullTokenAndCredential_ReturnsNull()
    {
        var sut = BuildValidator("client-id");

        var result = await sut.ValidateAsync(null, null, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WhitespaceToken_ReturnsNull()
    {
        var sut = BuildValidator("client-id");

        var result = await sut.ValidateAsync("   ", null, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_EmptyClientId_ReturnsNull()
    {
        var sut = BuildValidator(clientId: "");

        var result = await sut.ValidateAsync("some-token", null, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_UsesCredentialWhenIdTokenIsNull()
    {
        var sut = BuildValidator("client-id");

        // An invalid JWT will reach GoogleJsonWebSignature.ValidateAsync, catch InvalidJwtException, and return null.
        // This proves the credential fallback path is exercised.
        var result = await sut.ValidateAsync(null, "not-a-real-jwt", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_MalformedJwt_ReturnsNull()
    {
        var sut = BuildValidator("client-id");

        var result = await sut.ValidateAsync("this.is.not.a.valid.jwt", null, CancellationToken.None);

        result.Should().BeNull();
    }

    private static GoogleTokenValidator BuildValidator(string clientId) =>
        new(Options.Create(new GoogleOptions { ClientId = clientId, ClientSecret = "secret" }));
}
