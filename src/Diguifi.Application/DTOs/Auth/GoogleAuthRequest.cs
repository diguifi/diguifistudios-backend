namespace Diguifi.Application.DTOs.Auth;

public sealed class GoogleAuthRequest
{
    public string? Code { get; set; }
    public string? Credential { get; set; }
    public string? State { get; set; }
    public string? Scope { get; set; }
    public string? Authuser { get; set; }
    public string? Prompt { get; set; }
    public string? Error { get; set; }
    public string? CallbackPath { get; set; }
}
