using Gavel.ServiceDefaults.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gavel.Persistence;

internal sealed class DatabaseExceptionClassifier : IExceptionClassifier
{
    public ExceptionClassification? Classify(Exception exception) => exception switch
    {
        DbUpdateConcurrencyException => new(
            StatusCodes.Status409Conflict,
            "Concurrency",
            "The data was changed by another request at the same time. Please reload and try again."),
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } => new(
            StatusCodes.Status409Conflict,
            "Duplicate",
            "A record with the same unique value already exists."),
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.CheckViolation } } => new(
            StatusCodes.Status409Conflict,
            "ConstraintViolation",
            "The change would break a data rule."),
        _ => null,
    };
}
