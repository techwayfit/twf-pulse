using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace TechWayFit.Pulse.Web.Middleware;

public sealed class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
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
        catch (ValidationException ex)
        {
            await WriteProblemDetailsAsync(context, StatusCodes.Status400BadRequest, "Validation failed", ex.Message);
        }
        catch (ArgumentException ex)
        {
            await WriteProblemDetailsAsync(context, StatusCodes.Status400BadRequest, "Invalid request", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteProblemDetailsAsync(context, StatusCodes.Status400BadRequest, "Operation failed", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);
            await WriteProblemDetailsAsync(context, StatusCodes.Status500InternalServerError, "Internal server error", "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, int statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(problem);
    }
}
