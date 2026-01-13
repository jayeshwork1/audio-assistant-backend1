using AudioAssistant.Api.Utilities;
using System.Security.Claims;
using Xunit;

namespace AudioAssistant.Tests;

public class JwtTokenGeneratorTests
{
    private readonly JwtTokenGenerator _tokenGenerator;
    private const string SecretKey = "test-secret-key-minimum-32-characters-long";
    private const string Issuer = "TestIssuer";
    private const string Audience = "TestAudience";

    public JwtTokenGeneratorTests()
    {
        _tokenGenerator = new JwtTokenGenerator(SecretKey, Issuer, Audience, 60);
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidToken()
    {
        // Arrange
        var userId = 1;
        var email = "test@example.com";

        // Act
        var token = _tokenGenerator.GenerateToken(userId, email);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnClaimsPrincipal()
    {
        // Arrange
        var userId = 1;
        var email = "test@example.com";
        var token = _tokenGenerator.GenerateToken(userId, email);

        // Act
        var principal = _tokenGenerator.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        var emailClaim = principal.FindFirst(ClaimTypes.Email);
        
        Assert.NotNull(userIdClaim);
        Assert.NotNull(emailClaim);
        Assert.Equal(userId.ToString(), userIdClaim.Value);
        Assert.Equal(email, emailClaim.Value);
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var principal = _tokenGenerator.ValidateToken(invalidToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GenerateToken_ShouldIncludeUserId()
    {
        // Arrange
        var userId = 42;
        var email = "test@example.com";

        // Act
        var token = _tokenGenerator.GenerateToken(userId, email);
        var principal = _tokenGenerator.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        Assert.Equal(userId.ToString(), userIdClaim?.Value);
    }

    [Fact]
    public void GenerateToken_ShouldIncludeEmail()
    {
        // Arrange
        var userId = 1;
        var email = "user@example.com";

        // Act
        var token = _tokenGenerator.GenerateToken(userId, email);
        var principal = _tokenGenerator.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
        var emailClaim = principal.FindFirst(ClaimTypes.Email);
        Assert.Equal(email, emailClaim?.Value);
    }
}
