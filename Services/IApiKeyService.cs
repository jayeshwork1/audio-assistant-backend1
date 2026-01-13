namespace AudioAssistant.Api.Services;

/// <summary>
/// Interface for API key management services
/// </summary>
public interface IApiKeyService
{
    Task<(bool Success, string? Error)> StoreApiKeyAsync(int userId, string provider, string apiKey);
    Task<(bool Success, string? ApiKey, string? Error)> GetApiKeyAsync(int userId, string provider);
    Task<(bool Success, List<string>? Providers, string? Error)> GetUserProvidersAsync(int userId);
    Task<(bool Success, string? Error)> DeleteApiKeyAsync(int userId, string provider);
}
