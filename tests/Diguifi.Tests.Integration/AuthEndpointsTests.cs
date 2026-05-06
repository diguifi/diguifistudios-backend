using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Diguifi.Tests.Integration;

public sealed class AuthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact(Skip = "Requer SDK .NET instalado, banco/configuracao do host de teste e um token real em TEST_GOOGLE_ID_TOKEN.")]
    public async Task GoogleLogin_ShouldReturnOk()
    {
        var googleIdToken = Environment.GetEnvironmentVariable("TEST_GOOGLE_ID_TOKEN") ?? string.Empty;

        var response = await _client.PostAsJsonAsync("/api/auth/google", new
        {
            idToken = googleIdToken,
            credential = googleIdToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
