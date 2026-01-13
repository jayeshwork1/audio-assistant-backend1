using System.ComponentModel.DataAnnotations;

namespace AudioAssistant.Api.Models.DTOs;

public class StoreApiKeyRequest
{
    [Required]
    public string Provider { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;
}
