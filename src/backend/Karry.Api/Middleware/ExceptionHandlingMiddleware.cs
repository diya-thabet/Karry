using System.Net;
using System.Text.Json;
using Karry.Application.Common;

namespace Karry.Api.Middleware;

/// <summary>
/// Translates domain/application exceptions into consistent Problem Details responses and the
/// correct HTTP status code, logging unexpected failures.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private const string ProblemJsonType = "application/problem+json";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, exception.Message),
            ForbiddenException => (HttpStatusCode.Forbidden, exception.Message),
            AuthenticationException => (HttpStatusCode.Unauthorized, exception.Message),
            AccountLockedException => (HttpStatusCode.Locked, exception.Message),
            ConflictException => (HttpStatusCode.Conflict, exception.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred."),
        };

        if (status == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception during request {Path}", context.Request.Path);
        }

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = ProblemJsonType;

        var body = new
        {
            type = "https://tools.ietf.org/html/rfc9110",
            title,
            status = (int)status,
            traceId = context.TraceIdentifier,
        };

        await context.Response.WriteAsJsonAsync(body);
    }
}