using Diguifi.Application.Common;
using Diguifi.Application.DTOs.GameNotionPlayers;

namespace Diguifi.Application.Interfaces;

public interface IGameNotionPlayerService
{
    Task<IReadOnlyCollection<GameNotionPlayerResponse>> GetAllAsync(CancellationToken ct);
    Task<GameNotionPlayerResponse?> GetByIdAsync(string playerId, CancellationToken ct);
    Task<Result<GameNotionPlayerResponse>> CreateAsync(CreateGameNotionPlayerRequest request, CancellationToken ct);
    Task<Result<GameNotionPlayerResponse>> UpdateAsync(string playerId, UpdateGameNotionPlayerRequest request, CancellationToken ct);
    Task<Result<bool>> DeleteAsync(string playerId, CancellationToken ct);
    Task<Result<GameNotionPlayerResponse>> SetPlayerIdAsync(Guid userId, SetPlayerIdRequest request, CancellationToken ct);
}
