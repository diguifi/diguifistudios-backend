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

    [Fact(Skip = "Requer SDK .NET instalado e ajuste do host de teste para banco/configuração.")]
    public async Task GoogleLogin_ShouldReturnOk()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/google", new
        {
            idToken = "stub-token",
            credential = "stub-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
