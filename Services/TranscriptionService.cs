using AudioAssistant.Api.Data;
using AudioAssistant.Api.Models;
using AudioAssistant.Api.Services.Abstractions;
using AudioAssistant.Api.Services.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AudioAssistant.Api.Services;

/// <summary>
/// Service for orchestrating audio transcription across multiple providers
/// Implements fallback chain and provider selection logic
/// </summary>
public interface ITranscriptionService
{
    /// <summary>
    /// Transcribes audio data using the configured provider with fallback support
    /// </summary>
    Task<TranscriptionResult> TranscribeAsync(
        byte[] audioData,
        int userId,
        string language = "en",
        string? preferredProvider = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transcribes audio with streaming support
    /// </summary>
    IAsyncEnumerable<TranscriptionChunk> TranscribeStreamingAsync(
        Stream audioStream,
        int userId,
        string language = "en",
        string? preferredProvider = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available transcription providers
    /// </summary>
    Task<List<string>> GetAvailableProvidersAsync();

    /// <summary>
    /// Sets the user's preferred transcription provider
    /// </summary>
    Task SetPreferredProviderAsync(int userId, string provider);
}

public class TranscriptionService : ITranscriptionService
{
    private readonly AudioAssistantDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TranscriptionService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IApiKeyService _apiKeyService;

    public TranscriptionService(
        AudioAssistantDbContext context,
        IConfiguration configuration,
        ILogger<TranscriptionService> logger,
        IServiceProvider serviceProvider,
        IApiKeyService apiKeyService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _apiKeyService = apiKeyService;
    }

    public async Task<TranscriptionResult> TranscribeAsync(
        byte[] audioData,
        int userId,
        string language = "en",
        string? preferredProvider = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting transcription for user {UserId}, language: {Language}", userId, language);

            // Determine provider preference
            var providerPreference = preferredProvider ?? 
                await GetUserProviderPreference(userId) ?? 
                _configuration["TranscriptionSettings:DefaultProvider"] ?? 
                "GroqWhisper";

            _logger.LogInformation("Using provider preference: {Provider}", providerPreference);

            // Get provider fallback chain
            var providers = await GetProviderFallbackChain(userId, providerPreference);

            TranscriptionResult? lastResult = null;
            Exception? lastException = null;

            // Try each provider in the fallback chain
            foreach (var provider in providers)
            {
                try
                {
                    _logger.LogInformation("Attempting transcription with {Provider}", provider.ProviderName);

                    // Get API key if required
                    string? apiKey = null;
                    if (provider.RequiresApiKey)
                    {
                        var (success, retrievedKey, error) = await _apiKeyService.GetApiKeyAsync(userId, provider.ProviderName);
                        if (!success || string.IsNullOrEmpty(retrievedKey))
                        {
                            _logger.LogWarning("No API key available for {Provider}, trying next provider", provider.ProviderName);
                            continue;
                        }
                        apiKey = retrievedKey;
                    }

                    var isAvailable = await provider.IsAvailableAsync(apiKey);
                    if (!isAvailable)
                    {
                        _logger.LogWarning("Provider {Provider} is not available, trying next", provider.ProviderName);
                        continue;
                    }

                    var result = await provider.TranscribeAsync(audioData, apiKey, language, cancellationToken);
                    
                    // Validate result
                    if (string.IsNullOrEmpty(result.Text))
                    {
                        _logger.LogWarning("Provider {Provider} returned empty result, trying next", provider.ProviderName);
                        lastResult = result;
                        continue;
                    }

                    // Log successful transcription
                    var duration = DateTime.UtcNow - startTime;
                    _logger.LogInformation(
                        "Transcription successful with {Provider}: {TextLength} chars in {Duration}ms",
                        provider.ProviderName, result.Text.Length, duration.TotalMilliseconds);

                    // Log usage to transaction log
                    await LogUsageAsync(userId, provider.ProviderName, language, duration, result);

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Provider {Provider} failed, trying next provider", provider.ProviderName);
                    lastException = ex;
                    continue;
                }
            }

            // All providers failed
            if (lastException != null)
            {
                throw new InvalidOperationException(
                    $"All transcription providers failed. Last error: {lastException.Message}",
                    lastException);
            }

            throw new InvalidOperationException("All transcription providers returned empty results");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed for user {UserId}", userId);
            throw;
        }
    }

    public async IAsyncEnumerable<TranscriptionChunk> TranscribeStreamingAsync(
        Stream audioStream,
        int userId,
        string language = "en",
        string? preferredProvider = null,
        CancellationToken cancellationToken = default)
    {
        // Determine provider preference
        var providerPreference = preferredProvider ?? 
            await GetUserProviderPreference(userId) ?? 
            _configuration["TranscriptionSettings:DefaultProvider"] ?? 
            "GroqWhisper";

        // Get provider fallback chain
        var providers = await GetProviderFallbackChain(userId, providerPreference);

        // Try each provider (streaming typically uses single provider)
        foreach (var provider in providers)
        {
            try
            {
                // Get API key if required
                string? apiKey = null;
                if (provider.RequiresApiKey)
                {
                    var (success, retrievedKey, error) = await _apiKeyService.GetApiKeyAsync(userId, provider.ProviderName);
                    if (!success || string.IsNullOrEmpty(retrievedKey))
                    {
                        _logger.LogWarning("No API key available for {Provider}, trying next provider", provider.ProviderName);
                        continue;
                    }
                    apiKey = retrievedKey;
                }

                var isAvailable = await provider.IsAvailableAsync(apiKey);
                if (!isAvailable)
                {
                    _logger.LogWarning("Provider {Provider} is not available for streaming", provider.ProviderName);
                    continue;
                }

                await foreach (var chunk in provider.TranscribeStreamingAsync(audioStream, apiKey, language, cancellationToken))
                {
                    yield return chunk;
                }

                yield break; // Success, exit the loop
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider {Provider} streaming failed, trying next", provider.ProviderName);
                continue;
            }
        }

        throw new InvalidOperationException("All transcription providers failed for streaming");
    }

    public async Task<List<string>> GetAvailableProvidersAsync()
    {
        var providers = new List<ITranscriptionProvider>
        {
            _serviceProvider.GetRequiredService<GroqWhisperProvider>(),
            _serviceProvider.GetRequiredService<WhisperCppProvider>(),
            _serviceProvider.GetRequiredService<OpenAIWhisperProvider>()
        };

        var availableProviders = new List<string>();

        foreach (var provider in providers)
        {
            try
            {
                if (await provider.IsAvailableAsync())
                {
                    availableProviders.Add(provider.ProviderName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking availability for provider {Provider}", provider.ProviderName);
            }
        }

        return availableProviders;
    }

    public async Task SetPreferredProviderAsync(int userId, string provider)
    {
        var preferences = await _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        
        if (preferences == null)
        {
            preferences = new UserPreferences
            {
                UserId = userId,
                PreferredSTTProvider = provider,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserPreferences.Add(preferences);
        }
        else
        {
            preferences.PreferredSTTProvider = provider;
            preferences.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("User {UserId} preferred provider set to {Provider}", userId, provider);
    }

    private async Task<string?> GetUserProviderPreference(int userId)
    {
        var preferences = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);
        
        return preferences?.PreferredSTTProvider;
    }

    private async Task<List<ITranscriptionProvider>> GetProviderFallbackChain(
        int userId,
        string preferredProvider)
    {
        var chain = new List<ITranscriptionProvider>();

        // Get fallback settings from configuration or user preferences
        var fallbackConfig = _configuration.GetSection("TranscriptionSettings:FallbackChain");
        var fallbackOrder = fallbackConfig.Get<string[]>() 
            ?? new[] { "GroqWhisper", "WhisperCpp", "OpenAIWhisper" };

        // Get all available providers
        var groqProvider = _serviceProvider.GetRequiredService<GroqWhisperProvider>();
        var whisperCppProvider = _serviceProvider.GetRequiredService<WhisperCppProvider>();
        var openaiProvider = _serviceProvider.GetRequiredService<OpenAIWhisperProvider>();

        var availableProviders = new Dictionary<string, ITranscriptionProvider>
        {
            { "GroqWhisper", groqProvider },
            { "WhisperCpp", whisperCppProvider },
            { "OpenAIWhisper", openaiProvider }
        };

        // Build fallback chain starting with preferred provider
        if (availableProviders.TryGetValue(preferredProvider, out var preferred))
        {
            chain.Add(preferred);
        }

        // Add remaining providers in fallback order
        foreach (var providerName in fallbackOrder)
        {
            if (providerName != preferredProvider && 
                availableProviders.TryGetValue(providerName, out var provider))
            {
                chain.Add(provider);
            }
        }

        return chain;
    }

    private async Task LogUsageAsync(
        int userId,
        string provider,
        string language,
        TimeSpan duration,
        TranscriptionResult result)
    {
        try
        {
            var log = new TransactionLog
            {
                UserId = userId,
                TransactionType = "transcription",
                Provider = provider,
                RequestData = $"Language: {language}, Duration: {duration.TotalSeconds:F2}s",
                ResponseData = $"TextLength: {result.Text.Length}, Confidence: {result.Confidence:F2}",
                TokensUsed = result.Tokens,
                Status = "completed",
                Cost = CalculateCost(provider, duration),
                CreatedAt = DateTime.UtcNow
            };

            _context.TransactionLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging transcription usage");
        }
    }

    private decimal? CalculateCost(string provider, TimeSpan duration)
    {
        return provider switch
        {
            "GroqWhisper" => null, // Check current pricing
            "OpenAIWhisper" => (decimal)duration.TotalMinutes * 0.006m, // $0.006 per minute
            "WhisperCpp" => null, // Free/local
            _ => null
        };
    }
}
