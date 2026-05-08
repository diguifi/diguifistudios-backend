namespace Diguifi.Application.DTOs.GameNotionPlayers;

public sealed class CreateGameNotionPlayerRequest
{
    public string PlayerId { get; set; } = string.Empty;
    public DateTime LastPing { get; set; }
}
