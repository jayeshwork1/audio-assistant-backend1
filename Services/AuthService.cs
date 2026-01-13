using AudioAssistant.Api.Data;
using AudioAssistant.Api.Models;
using AudioAssistant.Api.Utilities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AudioAssistant.Api.Services;

/// <summary>
/// Service for handling authentication operations
/// </summary>
public class AuthService : IAuthService
{
    private readonly AudioAssistantDbContext _context;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AudioAssistantDbContext context,
        JwtTokenGenerator tokenGenerator,
        ILogger<AuthService> logger)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user
    /// </summary>
    public async Task<(bool Success, string? Token, string? Error)> RegisterAsync(string email, string password)
    {
        try
        {
            // Validate email format
            if (!new EmailAddressAttribute().IsValid(email))
            {
                return (false, null, "Invalid email format");
            }

            // Validate password strength
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                return (false, null, "Password must be at least 8 characters long");
            }

            // Check if user already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
            {
                return (false, null, "User with this email already exists");
            }

            // Create new user
            var user = new User
            {
                Email = email,
                PasswordHash = PasswordHasher.HashPassword(password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create default preferences
            var preferences = new UserPreferences
            {
                UserId = user.Id,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserPreferences.Add(preferences);
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = _tokenGenerator.GenerateToken(user.Id, user.Email);

            _logger.LogInformation("User registered successfully: {Email}", email);
            return (true, token, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user: {Email}", email);
            return (false, null, "An error occurred during registration");
        }
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    public async Task<(bool Success, string? Token, string? Error)> LoginAsync(string email, string password)
    {
        try
        {
            // Find user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return (false, null, "Invalid email or password");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return (false, null, "Account is inactive");
            }

            // Verify password
            if (!PasswordHasher.VerifyPassword(password, user.PasswordHash))
            {
                return (false, null, "Invalid email or password");
            }

            // Generate JWT token
            var token = _tokenGenerator.GenerateToken(user.Id, user.Email);

            _logger.LogInformation("User logged in successfully: {Email}", email);
            return (true, token, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in user: {Email}", email);
            return (false, null, "An error occurred during login");
        }
    }

    /// <summary>
    /// Refreshes an expired JWT token
    /// </summary>
    public async Task<(bool Success, string? Token, string? Error)> RefreshTokenAsync(string token)
    {
        try
        {
            var principal = _tokenGenerator.ValidateToken(token);
            if (principal == null)
            {
                return (false, null, "Invalid token");
            }

            var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            var emailClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.Email);

            if (userIdClaim == null || emailClaim == null)
            {
                return (false, null, "Invalid token claims");
            }

            var userId = int.Parse(userIdClaim.Value);
            var email = emailClaim.Value;

            // Verify user still exists and is active
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                return (false, null, "User not found or inactive");
            }

            // Generate new token
            var newToken = _tokenGenerator.GenerateToken(userId, email);

            _logger.LogInformation("Token refreshed for user: {Email}", email);
            return (true, newToken, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return (false, null, "An error occurred while refreshing token");
        }
    }
}
