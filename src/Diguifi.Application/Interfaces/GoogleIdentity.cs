namespace Diguifi.Application.Interfaces;

public sealed class GoogleIdentity
{
    public string Subject { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}
