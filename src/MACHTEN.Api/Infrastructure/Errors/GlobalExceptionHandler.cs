using MACHTEN.Api.Infrastructure.Logging;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MACHTEN.Api.Infrastructure.Errors;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private static readonly ProblemDetails InternalServerError = new()
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "An unexpected error occurred",
        Type = "https://tools.ietf.org/html/rfc7807"
    };

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogUnhandledException(exception);

        var problemDetails = exception switch
        {
            ArgumentException argEx => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid argument",
                Detail = argEx.Message,
                Type = "https://tools.ietf.org/html/rfc7807"
            },
            KeyNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found",
                Type = "https://tools.ietf.org/html/rfc7807"
            },
            OperationCanceledException => new ProblemDetails
            {
                Status = StatusCodes.Status499ClientClosedRequest,
                Title = "Request cancelled",
                Type = "https://tools.ietf.org/html/rfc7807"
            },
            _ => InternalServerError
        };

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            AppSerializerContext.Default.Options,
            cancellationToken);

        return true;
    }
}
