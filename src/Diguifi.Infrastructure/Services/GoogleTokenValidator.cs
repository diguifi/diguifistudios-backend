using Diguifi.Application.Interfaces;
using Diguifi.Infrastructure.Options;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace Diguifi.Infrastructure.Services;

public sealed class GoogleTokenValidator(IOptions<GoogleOptions> googleOptions) : IGoogleTokenValidator
{
    private readonly GoogleOptions _googleOptions = googleOptions.Value;

    public async Task<GoogleIdentity?> ValidateAsync(string? idToken, string? credential, CancellationToken cancellationToken)
    {
        var token = idToken ?? credential;
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(_googleOptions.ClientId))
        {
            return null;
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                token,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_googleOptions.ClientId]
                });

            return new GoogleIdentity
            {
                Subject = payload.Subject,
                Email = payload.Email,
                Name = payload.Name ?? payload.Email,
                FirstName = payload.GivenName ?? payload.Name ?? payload.Email,
                AvatarUrl = payload.Picture ?? string.Empty
            };
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
