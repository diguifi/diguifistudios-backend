namespace Diguifi.Application.Interfaces;

public interface IGoogleTokenValidator
{
    Task<GoogleIdentity?> ValidateAsync(string? code, string? credential, CancellationToken cancellationToken);
}
