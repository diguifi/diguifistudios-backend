using System.ComponentModel.DataAnnotations;

namespace Diguifi.Application.DTOs.Auth;

public sealed class GoogleAuthRequest
{
    [Required]
    public string? IdToken { get; set; }

    [Required]
    public string? Credential { get; set; }
}
