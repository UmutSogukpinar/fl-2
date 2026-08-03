using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using FantasyLeague.Application.Common.Exceptions;

namespace FantasyLeague.WebApi.ExceptionHandlers;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> _logger,
    IProblemDetailsService _problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var exceptionDetails = GetExceptionDetails(exception);
        LogException(httpContext, exception, exceptionDetails.StatusCode);
        SetResponseStatusCode(httpContext, exceptionDetails.StatusCode);

        var problemDetails = CreateProblemDetails(httpContext, exceptionDetails);

        var context = CreateProblemDetailsContext(
            httpContext,
            problemDetails,
            exception
        );

        return await _problemDetailsService.TryWriteAsync(context);
    }

    private void LogException(
        HttpContext httpContext,
        Exception exception,
        int statusCode)
    {
        if (statusCode < StatusCodes.Status500InternalServerError)
        {
            _logger.LogWarning(
                "Request {Method} {Path} was rejected with {StatusCode}. " +
                "TraceId: {TraceId}. Reason: {Reason}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                statusCode,
                httpContext.TraceIdentifier,
                exception.Message);
            return;
        }

        _logger.LogError(
            exception,
            "An unhandled exception occurred while processing {Method} {Path}. " +
            "TraceId: {TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);
    }

    private static ExceptionDetails GetExceptionDetails(Exception exception)
    {
        return exception switch
        {
            BadRequestException => new(
                StatusCodes.Status400BadRequest,
                "Bad request",
                exception.Message
            ),
            UnauthorizedException => new(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                exception.Message
            ),
            ForbiddenException => new(
                StatusCodes.Status403Forbidden,
                "Forbidden",
                exception.Message
            ),
            NotFoundException => new(
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message
            ),
            ConflictException => new(
                StatusCodes.Status409Conflict,
                "Conflict",
                exception.Message
            ),
            ExternalServiceException => new(
                StatusCodes.Status502BadGateway,
                "External service error",
                exception.Message
            ),
            _ => new(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "The server was unable to complete the request."
            )
        };
    }

    private static void SetResponseStatusCode(
        HttpContext httpContext,
        int statusCode
    )
    {
        httpContext.Response.StatusCode = statusCode;
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        ExceptionDetails exceptionDetails
    )
    {
        var problemDetails = new ProblemDetails
        {
            Status = exceptionDetails.StatusCode,
            Title = exceptionDetails.Title,
            Detail = exceptionDetails.Detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        return problemDetails;
    }

    private static ProblemDetailsContext CreateProblemDetailsContext(
        HttpContext httpContext,
        ProblemDetails problemDetails,
        Exception exception
    )
    {
        return new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        };
    }

    private sealed record ExceptionDetails(
        int StatusCode,
        string Title,
        string Detail
    );
}
