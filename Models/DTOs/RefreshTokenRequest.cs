using System.ComponentModel.DataAnnotations;

namespace AudioAssistant.Api.Models.DTOs;

public class RefreshTokenRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
