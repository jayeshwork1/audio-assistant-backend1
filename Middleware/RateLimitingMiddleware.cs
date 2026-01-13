using System.Collections.Concurrent;
using System.Security.Claims;

namespace AudioAssistant.Api.Middleware;

/// <summary>
/// Middleware for basic rate limiting per user
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, UserRateLimit> _rateLimits = new();
    private readonly int _requestsPerMinute;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _requestsPerMinute = configuration.GetValue<int>("RateLimiting:RequestsPerMinute", 100);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        // Skip rate limiting for unauthenticated requests or auth endpoints
        if (string.IsNullOrEmpty(userId) || context.Request.Path.StartsWithSegments("/api/auth"))
        {
            await _next(context);
            return;
        }

        var rateLimit = _rateLimits.GetOrAdd(userId, _ => new UserRateLimit());

        bool rateLimitExceeded = false;
        lock (rateLimit)
        {
            var now = DateTime.UtcNow;
            
            // Remove old requests outside the time window
            rateLimit.RequestTimestamps.RemoveAll(t => (now - t).TotalMinutes > 1);
            
            // Check if rate limit exceeded
            if (rateLimit.RequestTimestamps.Count >= _requestsPerMinute)
            {
                rateLimitExceeded = true;
            }
            else
            {
                // Add current request timestamp
                rateLimit.RequestTimestamps.Add(now);
            }
        }

        if (rateLimitExceeded)
        {
            _logger.LogWarning("Rate limit exceeded for user {UserId}", userId);
            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                Error = "Too Many Requests",
                Message = $"Rate limit of {_requestsPerMinute} requests per minute exceeded",
                StatusCode = 429
            });
            return;
        }

        await _next(context);
    }

    private class UserRateLimit
    {
        public List<DateTime> RequestTimestamps { get; } = new();
    }
}
