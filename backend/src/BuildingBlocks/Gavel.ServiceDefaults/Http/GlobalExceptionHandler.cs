using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Gavel.ServiceDefaults.Http;

internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    IEnumerable<IExceptionClassifier> classifiers,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private const int ClientClosedRequest = 499;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var classification = classifiers.Select(c => c.Classify(exception)).FirstOrDefault(c => c is not null)
            ?? Classify(exception, httpContext);

        if (classification.Status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            LogHandled(logger, httpContext.Request.Method, httpContext.Request.Path, classification.Code);
        }

        return await ProblemWriter.WriteAsync(
            problemDetails,
            httpContext,
            exception,
            classification.Status,
            classification.Code,
            classification.Detail);
    }

    private static ExceptionClassification Classify(Exception exception, HttpContext httpContext) => exception switch
    {
        BadHttpRequestException bad => new(bad.StatusCode, "BadRequest", "The request could not be read."),
        UnauthorizedAccessException => new(StatusCodes.Status401Unauthorized, "Unauthorized", "Authentication is required."),
        OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested =>
            new(ClientClosedRequest, "Cancelled", "The request was cancelled."),
        _ => new(StatusCodes.Status500InternalServerError, "ServerError", "An unexpected error occurred."),
    };

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception for {Method} {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string method, PathString path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request {Method} {Path} failed with {Code}")]
    private static partial void LogHandled(ILogger logger, string method, PathString path, string code);
}
