using AudioAssistant.Api.Models.DTOs;
using AudioAssistant.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AudioAssistant.Api.Controllers;

/// <summary>
/// Controller for API key management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApiKeyController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeyController> _logger;

    public ApiKeyController(IApiKeyService apiKeyService, ILogger<ApiKeyController> logger)
    {
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    /// <summary>
    /// Store an encrypted API key for a provider
    /// </summary>
    [HttpPost("store")]
    public async Task<IActionResult> StoreApiKey([FromBody] StoreApiKeyRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var (success, error) = await _apiKeyService.StoreApiKeyAsync(userId.Value, request.Provider, request.ApiKey);

        if (!success)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Failed to store API key",
                Message = error,
                StatusCode = 400
            });
        }

        return Ok(new { Message = "API key stored successfully" });
    }

    /// <summary>
    /// Get list of configured providers for the user
    /// </summary>
    [HttpGet("providers")]
    public async Task<IActionResult> GetProviders()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var (success, providers, error) = await _apiKeyService.GetUserProvidersAsync(userId.Value);

        if (!success)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Failed to retrieve providers",
                Message = error,
                StatusCode = 400
            });
        }

        return Ok(new { Providers = providers });
    }

    /// <summary>
    /// Delete an API key for a provider
    /// </summary>
    [HttpDelete("{provider}")]
    public async Task<IActionResult> DeleteApiKey(string provider)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var (success, error) = await _apiKeyService.DeleteApiKeyAsync(userId.Value, provider);

        if (!success)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Failed to delete API key",
                Message = error,
                StatusCode = 400
            });
        }

        return Ok(new { Message = "API key deleted successfully" });
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}
