using Diguifi.Application.Common;
using Diguifi.Application.DTOs.Auth;

namespace Diguifi.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> AuthenticateWithGoogleAsync(GoogleAuthRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken);
    Task<Result<RefreshResponse>> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken);
    Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken cancellationToken);
    Task<Result<UserProfileResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}
