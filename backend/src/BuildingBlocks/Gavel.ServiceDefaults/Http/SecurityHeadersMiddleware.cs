using Microsoft.AspNetCore.Http;

namespace Gavel.ServiceDefaults.Http;

internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private static readonly Func<object, Task> ApplyHeaders = state =>
    {
        var headers = ((HttpContext)state).Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";
        headers["X-Robots-Tag"] = "noindex";
        if (!headers.ContainsKey("Cache-Control"))
        {
            headers.CacheControl = "no-store";
        }

        return Task.CompletedTask;
    };

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(ApplyHeaders, context);
        return next(context);
    }
}
