using AudioAssistant.Api.Data;
using AudioAssistant.Api.Models;
using AudioAssistant.Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace AudioAssistant.Api.Services;

/// <summary>
/// Service for managing encrypted API keys
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private readonly AudioAssistantDbContext _context;
    private readonly EncryptionService _encryptionService;
    private readonly ILogger<ApiKeyService> _logger;

    public ApiKeyService(
        AudioAssistantDbContext context,
        EncryptionService encryptionService,
        ILogger<ApiKeyService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    /// <summary>
    /// Stores an encrypted API key for a user and provider
    /// </summary>
    public async Task<(bool Success, string? Error)> StoreApiKeyAsync(int userId, string provider, string apiKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                return (false, "Provider name is required");
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return (false, "API key is required");
            }

            // Check if user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            // Encrypt the API key
            var encryptedKey = _encryptionService.Encrypt(apiKey);

            // Check if API key already exists for this provider
            var existingKey = await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.UserId == userId && k.Provider == provider);

            if (existingKey != null)
            {
                // Update existing key
                existingKey.EncryptedKey = encryptedKey;
                existingKey.IsActive = true;
            }
            else
            {
                // Create new key
                var newKey = new ApiKey
                {
                    UserId = userId,
                    Provider = provider,
                    EncryptedKey = encryptedKey,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ApiKeys.Add(newKey);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("API key stored for user {UserId}, provider {Provider}", userId, provider);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing API key for user {UserId}, provider {Provider}", userId, provider);
            return (false, "An error occurred while storing the API key");
        }
    }

    /// <summary>
    /// Retrieves and decrypts an API key for a user and provider
    /// </summary>
    public async Task<(bool Success, string? ApiKey, string? Error)> GetApiKeyAsync(int userId, string provider)
    {
        try
        {
            var apiKey = await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.UserId == userId && k.Provider == provider && k.IsActive);

            if (apiKey == null)
            {
                return (false, null, "API key not found for this provider");
            }

            // Decrypt the API key
            var decryptedKey = _encryptionService.Decrypt(apiKey.EncryptedKey);

            return (true, decryptedKey, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API key for user {UserId}, provider {Provider}", userId, provider);
            return (false, null, "An error occurred while retrieving the API key");
        }
    }

    /// <summary>
    /// Gets a list of providers for which the user has stored API keys
    /// </summary>
    public async Task<(bool Success, List<string>? Providers, string? Error)> GetUserProvidersAsync(int userId)
    {
        try
        {
            var providers = await _context.ApiKeys
                .Where(k => k.UserId == userId && k.IsActive)
                .Select(k => k.Provider)
                .ToListAsync();

            return (true, providers, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving providers for user {UserId}", userId);
            return (false, null, "An error occurred while retrieving providers");
        }
    }

    /// <summary>
    /// Deletes an API key for a user and provider
    /// </summary>
    public async Task<(bool Success, string? Error)> DeleteApiKeyAsync(int userId, string provider)
    {
        try
        {
            var apiKey = await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.UserId == userId && k.Provider == provider);

            if (apiKey == null)
            {
                return (false, "API key not found for this provider");
            }

            _context.ApiKeys.Remove(apiKey);
            await _context.SaveChangesAsync();

            _logger.LogInformation("API key deleted for user {UserId}, provider {Provider}", userId, provider);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting API key for user {UserId}, provider {Provider}", userId, provider);
            return (false, "An error occurred while deleting the API key");
        }
    }
}
