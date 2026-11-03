using Gavel.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace Gavel.ServiceDefaults.Http;

public static class ResultExtensions
{
    public static IResult ToProblem(this Error error)
    {
        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.TooManyRequests => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status400BadRequest,
        };

        return TypedResults.Problem(
            detail: error.Message,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }

    public static IResult ToHttpResult(this Result result, Func<IResult>? onSuccess = null) =>
        result.IsSuccess ? onSuccess?.Invoke() ?? TypedResults.NoContent() : result.Error!.ToProblem();

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null) =>
        result.IsSuccess
            ? onSuccess?.Invoke(result.Value) ?? TypedResults.Ok(result.Value)
            : result.Error!.ToProblem();
}
