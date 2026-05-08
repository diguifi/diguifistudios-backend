using Diguifi.Application.Common;
using Diguifi.Application.DTOs.GameNotionPlayers;
using Diguifi.Application.Interfaces;
using Diguifi.Domain.Entities;
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
}
