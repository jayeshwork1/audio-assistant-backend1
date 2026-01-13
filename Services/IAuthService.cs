namespace AudioAssistant.Api.Services;

/// <summary>
/// Interface for authentication services
/// </summary>
public interface IAuthService
{
    Task<(bool Success, string? Token, string? Error)> RegisterAsync(string email, string password);
    Task<(bool Success, string? Token, string? Error)> LoginAsync(string email, string password);
    Task<(bool Success, string? Token, string? Error)> RefreshTokenAsync(string token);
}
