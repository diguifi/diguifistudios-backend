using Diguifi.Application.Common;
using Diguifi.Application.DTOs.GameNotionPlayers;
using Diguifi.Application.Interfaces;
using Diguifi.Domain.Entities;
using Diguifi.Domain.Enums;
using Diguifi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Diguifi.Infrastructure.Services;

public sealed class GameNotionPlayerService(AppDbContext dbContext) : IGameNotionPlayerService
{
    public async Task<IReadOnlyCollection<GameNotionPlayerResponse>> GetAllAsync(CancellationToken ct)
        => await dbContext.GameNotionPlayers
            .AsNoTracking()
            .Select(p => new GameNotionPlayerResponse { PlayerId = p.PlayerId, LastPing = p.LastPing })
            .ToListAsync(ct);

    public async Task<GameNotionPlayerResponse?> GetByIdAsync(string playerId, CancellationToken ct)
        => await dbContext.GameNotionPlayers
            .AsNoTracking()
            .Where(p => p.PlayerId == playerId)
            .Select(p => new GameNotionPlayerResponse { PlayerId = p.PlayerId, LastPing = p.LastPing })
            .FirstOrDefaultAsync(ct);

    public async Task<Result<GameNotionPlayerResponse>> CreateAsync(CreateGameNotionPlayerRequest request, CancellationToken ct)
    {
        var exists = await dbContext.GameNotionPlayers.AnyAsync(p => p.PlayerId == request.PlayerId, ct);
        if (exists)
            return Result<GameNotionPlayerResponse>.Failure("player_already_exists", "Já existe um player com esse ID.");

        var entity = new GameNotionPlayer { PlayerId = request.PlayerId, LastPing = request.LastPing };
        dbContext.GameNotionPlayers.Add(entity);
        await dbContext.SaveChangesAsync(ct);

        return Result<GameNotionPlayerResponse>.Success(new GameNotionPlayerResponse { PlayerId = entity.PlayerId, LastPing = entity.LastPing });
    }

    public async Task<Result<GameNotionPlayerResponse>> UpdateAsync(string playerId, UpdateGameNotionPlayerRequest request, CancellationToken ct)
    {
        var entity = await dbContext.GameNotionPlayers.FirstOrDefaultAsync(p => p.PlayerId == playerId, ct);
        if (entity is null)
            return Result<GameNotionPlayerResponse>.Failure("player_not_found", "Player não encontrado.");

        entity.LastPing = request.LastPing;
        await dbContext.SaveChangesAsync(ct);

        return Result<GameNotionPlayerResponse>.Success(new GameNotionPlayerResponse { PlayerId = entity.PlayerId, LastPing = entity.LastPing });
    }

    public async Task<Result<bool>> DeleteAsync(string playerId, CancellationToken ct)
    {
        var entity = await dbContext.GameNotionPlayers.FirstOrDefaultAsync(p => p.PlayerId == playerId, ct);
        if (entity is null)
            return Result<bool>.Failure("player_not_found", "Player não encontrado.");

        dbContext.GameNotionPlayers.Remove(entity);
        await dbContext.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<GameNotionPlayerResponse>> SetPlayerIdAsync(Guid userId, SetPlayerIdRequest request, CancellationToken ct)
    {
        var hasBundleAccess = await dbContext.Orders.AnyAsync(
            o => o.UserId == userId &&
                 o.Status == OrderStatus.Paid &&
                 dbContext.Bundles.Any(b => b.ProductId == o.ProductId && b.BundleType == BundleType.GameNotion), ct);

        if (!hasBundleAccess)
            return Result<GameNotionPlayerResponse>.Failure("no_bundle_access", "É necessário ter comprado um bundle para definir um Player ID.");

        // null UserId = admin-created, unowned — not considered "taken"
        var takenByOther = await dbContext.GameNotionPlayers.AnyAsync(
            p => p.PlayerId == request.PlayerId && p.UserId != null && p.UserId != userId, ct);

        if (takenByOther)
            return Result<GameNotionPlayerResponse>.Failure("player_id_taken", "Este Player ID já está em uso.");

        var existing = await dbContext.GameNotionPlayers.FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (existing?.PlayerId == request.PlayerId)
            return Result<GameNotionPlayerResponse>.Success(new GameNotionPlayerResponse { PlayerId = existing.PlayerId, LastPing = existing.LastPing });

        if (existing is not null)
            dbContext.GameNotionPlayers.Remove(existing);

        var target = await dbContext.GameNotionPlayers.FirstOrDefaultAsync(p => p.PlayerId == request.PlayerId, ct);

        if (target is not null)
        {
            target.UserId = userId;
            target.LastPing = DateTime.UtcNow;
        }
        else
        {
            target = new GameNotionPlayer { PlayerId = request.PlayerId, UserId = userId, LastPing = DateTime.UtcNow };
            dbContext.GameNotionPlayers.Add(target);
        }

        await dbContext.SaveChangesAsync(ct);
        return Result<GameNotionPlayerResponse>.Success(new GameNotionPlayerResponse { PlayerId = target.PlayerId, LastPing = target.LastPing });
    }
}
