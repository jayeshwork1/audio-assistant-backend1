namespace AudioAssistant.Api.Models.DTOs;

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? Details { get; set; }
    public int StatusCode { get; set; }
}
