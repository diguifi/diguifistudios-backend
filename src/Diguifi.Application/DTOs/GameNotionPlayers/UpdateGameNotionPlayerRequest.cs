namespace Diguifi.Application.DTOs.GameNotionPlayers;

public sealed class UpdateGameNotionPlayerRequest
{
    public string? NewPlayerId { get; set; }
    public DateTime LastPing { get; set; }
}
