using System.Text.Json.Serialization;

namespace Diguifi.Application.DTOs.Auth;

public sealed class RefreshResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }

    [JsonIgnore]
    public string RefreshToken { get; set; } = string.Empty;
}
