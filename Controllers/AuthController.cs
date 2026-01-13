using AudioAssistant.Api.Models.DTOs;
using AudioAssistant.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AudioAssistant.Api.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, token, error) = await _authService.RegisterAsync(request.Email, request.Password);

        if (!success)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Registration failed",
                Message = error,
                StatusCode = 400
            });
        }

        return Ok(new AuthResponse
        {
            Token = token!,
            Email = request.Email
        });
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, token, error) = await _authService.LoginAsync(request.Email, request.Password);

        if (!success)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Login failed",
                Message = error,
                StatusCode = 401
            });
        }

        return Ok(new AuthResponse
        {
            Token = token!,
            Email = request.Email
        });
    }

    /// <summary>
    /// Refresh an expired token
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, token, error) = await _authService.RefreshTokenAsync(request.Token);

        if (!success)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Token refresh failed",
                Message = error,
                StatusCode = 401
            });
        }

        return Ok(new { Token = token });
    }
}
