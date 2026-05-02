using Diguifi.Application.Interfaces;

namespace Diguifi.Infrastructure.Services;

public sealed class GoogleTokenValidatorStub : IGoogleTokenValidator
{
    public Task<GoogleIdentity?> ValidateAsync(string? code, string? credential, CancellationToken cancellationToken)
    {
        var token = credential ?? code;
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult<GoogleIdentity?>(null);
        }

        return Task.FromResult<GoogleIdentity?>(new GoogleIdentity
        {
            Subject = $"stub-{token.GetHashCode()}",
            Email = "stub.user@example.com",
            Name = "Stub User",
            FirstName = "Stub",
            AvatarUrl = "https://example.com/avatar.png"
        });
    }
}
