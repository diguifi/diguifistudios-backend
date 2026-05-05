namespace Diguifi.Application.Interfaces;

public interface IGoogleTokenValidator
{
    Task<GoogleIdentity?> ValidateAsync(string? idToken, string? credential, CancellationToken cancellationToken);
}
