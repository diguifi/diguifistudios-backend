using Diguifi.Application.Common;
using Diguifi.Application.DTOs.Auth;
using Diguifi.Application.Interfaces;
using Diguifi.Domain.Entities;
using Diguifi.Infrastructure.Options;
using Diguifi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Diguifi.Infrastructure.Services;

public sealed class AuthService(
    AppDbContext dbContext,
    IGoogleTokenValidator googleTokenValidator,
    ITokenService tokenService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<Result<AuthResponse>> AuthenticateWithGoogleAsync(
        GoogleAuthRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var identity = await googleTokenValidator.ValidateAsync(request.IdToken, request.Credential, cancellationToken);
        if (identity is null)
        {
            return Result<AuthResponse>.Failure("invalid_google_token", "Nao foi possivel validar o token do Google.");
        }

        var user = await dbContext.Users
            .Include(x => x.RefreshTokens)
            .FirstOrDefaultAsync(x => x.GoogleSubject == identity.Subject || x.Email == identity.Email, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                GoogleSubject = identity.Subject,
                Email = identity.Email,
                Name = identity.Name,
                FirstName = identity.FirstName,
                AvatarUrl = identity.AvatarUrl
            };

            dbContext.Users.Add(user);
        }
        else
        {
            user.GoogleSubject = identity.Subject;
            user.Email = identity.Email;
            user.Name = identity.Name;
            user.FirstName = identity.FirstName;
            user.AvatarUrl = identity.AvatarUrl;
            user.LastLoginAt = DateTimeOffset.UtcNow;
        }

        var refreshTokenValue = tokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            User = user,
            TokenHash = tokenService.ComputeHash(refreshTokenValue),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            UserAgent = userAgent,
            IpAddress = ipAddress
        };

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.CreateAccessToken(user);
        return Result<AuthResponse>.Success(new AuthResponse
        {
            User = MapUser(user),
            AccessToken = accessToken.Token,
            ExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshTokenValue
        });
    }

    public async Task<Result<RefreshResponse>> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<RefreshResponse>.Failure("missing_refresh_token", "Refresh token nao informado.");
        }

        var tokenHash = tokenService.ComputeHash(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive || storedToken.User is null)
        {
            return Result<RefreshResponse>.Failure("invalid_refresh_token", "Refresh token invalido, expirado ou revogado.");
        }

        storedToken.RevokedAt = DateTimeOffset.UtcNow;
        var rotatedValue = tokenService.GenerateRefreshToken();
        var rotatedToken = new RefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = tokenService.ComputeHash(rotatedValue),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            UserAgent = userAgent,
            IpAddress = ipAddress
        };
        storedToken.ReplacedByTokenId = rotatedToken.Id;

        dbContext.RefreshTokens.Add(rotatedToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.CreateAccessToken(storedToken.User);
        return Result<RefreshResponse>.Success(new RefreshResponse
        {
            AccessToken = accessToken.Token,
            ExpiresAt = accessToken.ExpiresAt,
            RefreshToken = rotatedValue
        });
    }

    public async Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<bool>.Success(true);
        }

        var tokenHash = tokenService.ComputeHash(refreshToken);
        var storedToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (storedToken is null)
        {
            return Result<bool>.Success(true);
        }

        storedToken.RevokedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<UserProfileResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        return user is null
            ? Result<UserProfileResponse>.Failure("user_not_found", "Usuario autenticado nao encontrado.")
            : Result<UserProfileResponse>.Success(MapUser(user));
    }

    private static UserProfileResponse MapUser(User user) => new()
    {
        Id = user.Id.ToString(),
        Email = user.Email,
        Name = user.Name,
        FirstName = user.FirstName,
        AvatarUrl = user.AvatarUrl,
        IsAdmin = JwtTokenService.AdminEmails.Contains(user.Email)
    };
}
