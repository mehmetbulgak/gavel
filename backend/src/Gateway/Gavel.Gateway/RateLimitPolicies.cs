using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Gavel.ServiceDefaults.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Gavel.Gateway;

internal static class RateLimitPolicies
{
    public const string SignIn = "sign-in";
    public const string Session = "session";
    public const string Bids = "bids";
    public const string Listings = "listings";
    public const string Api = "api";
    public const string Realtime = "realtime";

    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitOptions>()
            .BindConfiguration(RateLimitOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                return ValueTask.CompletedTask;
            };

            options.AddPolicy(SignIn, http => Window(http, ClientAddress(http), o => o.SignIn));
            options.AddPolicy(Session, http => Window(http, ClientAddress(http), o => o.Session));
            options.AddPolicy(Bids, http => Window(http, UserOrClient(http), o => o.Bids));
            options.AddPolicy(Listings, http => Window(http, UserOrClient(http), o => o.Listings));
            options.AddPolicy(Api, http => Window(http, UserOrClient(http), o => o.Api));
            options.AddPolicy(Realtime, http => Window(http, ClientAddress(http), o => o.Realtime));
        });

        return services;
    }

    private static RateLimitPartition<string> Window(HttpContext http, string key, Func<RateLimitOptions, int> limit)
    {
        var options = http.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limit(options),
            Window = TimeSpan.FromSeconds(options.WindowSeconds),
            QueueLimit = 0,
        });
    }

    private static string ClientAddress(HttpContext http) => $"ip:{http.Connection.RemoteIpAddress}";

    private static string UserOrClient(HttpContext http) =>
        http.User.FindFirstValue(GavelClaims.Subject) is { } userId ? $"user:{userId}" : ClientAddress(http);
}
