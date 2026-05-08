namespace Diguifi.Domain.Entities;

public sealed class GameNotionPlayer
{
    public string PlayerId { get; set; } = string.Empty;
    public DateTime LastPing { get; set; }
}
