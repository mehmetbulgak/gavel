using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gavel.ServiceDefaults.Http;

public static class ProblemWriter
{
    public static ValueTask<bool> WriteAsync(
        IProblemDetailsService problemDetails,
        HttpContext httpContext,
        Exception exception,
        int status,
        string code,
        string detail)
    {
        httpContext.Response.StatusCode = status;
        return problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Detail = detail,
                Extensions = { ["code"] = code },
            },
        });
    }
}
