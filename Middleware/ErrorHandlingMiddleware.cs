using AudioAssistant.Api.Models.DTOs;
using System.Net;
using System.Text.Json;

namespace AudioAssistant.Api.Middleware;

/// <summary>
/// Middleware for centralized error handling
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        var errorResponse = new ErrorResponse
        {
            Error = "Internal Server Error",
            Message = exception.Message,
            Details = exception.StackTrace,
            StatusCode = (int)code
        };

        // Customize error response based on exception type
        if (exception is UnauthorizedAccessException)
        {
            code = HttpStatusCode.Unauthorized;
            errorResponse.Error = "Unauthorized";
            errorResponse.StatusCode = (int)code;
        }
        else if (exception is ArgumentException)
        {
            code = HttpStatusCode.BadRequest;
            errorResponse.Error = "Bad Request";
            errorResponse.StatusCode = (int)code;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        var result = JsonSerializer.Serialize(errorResponse);
        return context.Response.WriteAsync(result);
    }
}
