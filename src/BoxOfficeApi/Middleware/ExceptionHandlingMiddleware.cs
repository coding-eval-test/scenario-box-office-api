using System.Net.Mime;
using BoxOfficeApi.Services.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BoxOfficeApi.Middleware;

/// <summary>Translates domain exceptions into RFC 7807 ProblemDetails responses.</summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> _logger)
    {
        _next = next;
        this._logger = _logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            _logger.LogInformation(ex, "Resource not found for {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Not Found", ex.Message);
        }
        catch (ConflictException ex)
        {
            _logger.LogInformation(ex, "Conflicting request for {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, "Conflict", ex.Message);
        }
        catch (PaymentFailedException ex)
        {
            _logger.LogInformation(ex, "Payment declined for {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status402PaymentRequired, "Payment Required", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "Internal Server Error", "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.Clear();
        context.Response.StatusCode = status;

        // WriteAsJsonAsync resets the content type unless it is passed explicitly.
        await context.Response.WriteAsJsonAsync(
            problem, options: null, contentType: MediaTypeNames.Application.ProblemJson);
    }
}
