using AudioAssistant.Api.Data;
using AudioAssistant.Api.Models;
using AudioAssistant.Api.Services;
using AudioAssistant.Api.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AudioAssistant.Tests;

public class AuthServiceTests
{
    private readonly AudioAssistantDbContext _context;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AudioAssistantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AudioAssistantDbContext(options);
        _tokenGenerator = new JwtTokenGenerator(
            "test-secret-key-minimum-32-characters-long",
            "TestIssuer",
            "TestAudience",
            60);
        _loggerMock = new Mock<ILogger<AuthService>>();
        _authService = new AuthService(_context, _tokenGenerator, _loggerMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Password123!";

        // Act
        var (success, token, error) = await _authService.RegisterAsync(email, password);

        // Assert
        Assert.True(success);
        Assert.NotNull(token);
        Assert.Null(error);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        var email = "invalid-email";
        var password = "Password123!";

        // Act
        var (success, token, error) = await _authService.RegisterAsync(email, password);

        // Assert
        Assert.False(success);
        Assert.Null(token);
        Assert.NotNull(error);
        Assert.Contains("Invalid email", error);
    }

    [Fact]
    public async Task RegisterAsync_WithShortPassword_ShouldFail()
    {
        // Arrange
        var email = "test@example.com";
        var password = "short";

        // Act
        var (success, token, error) = await _authService.RegisterAsync(email, password);

        // Assert
        Assert.False(success);
        Assert.Null(token);
        Assert.NotNull(error);
        Assert.Contains("8 characters", error);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldFail()
    {
        // Arrange
        var email = "existing@example.com";
        var password = "Password123!";
        await _authService.RegisterAsync(email, password);

        // Act
        var (success, token, error) = await _authService.RegisterAsync(email, password);

        // Assert
        Assert.False(success);
        Assert.Null(token);
        Assert.NotNull(error);
        Assert.Contains("already exists", error);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        var email = "login@example.com";
        var password = "Password123!";
        await _authService.RegisterAsync(email, password);

        // Act
        var (success, token, error) = await _authService.LoginAsync(email, password);

        // Assert
        Assert.True(success);
        Assert.NotNull(token);
        Assert.Null(error);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "Password123!";

        // Act
        var (success, token, error) = await _authService.LoginAsync(email, password);

        // Assert
        Assert.False(success);
        Assert.Null(token);
        Assert.NotNull(error);
        Assert.Contains("Invalid email or password", error);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldFail()
    {
        // Arrange
        var email = "user@example.com";
        var password = "Password123!";
        var wrongPassword = "WrongPassword!";
        await _authService.RegisterAsync(email, password);

        // Act
        var (success, token, error) = await _authService.LoginAsync(email, wrongPassword);

        // Assert
        Assert.False(success);
        Assert.Null(token);
        Assert.NotNull(error);
        Assert.Contains("Invalid email or password", error);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUserPreferences()
    {
        // Arrange
        var email = "prefs@example.com";
        var password = "Password123!";

        // Act
        await _authService.RegisterAsync(email, password);

        // Assert
        var user = await _context.Users.Include(u => u.Preferences)
            .FirstOrDefaultAsync(u => u.Email == email);
        
        Assert.NotNull(user);
        Assert.NotNull(user.Preferences);
    }
}
