using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Gavel.ServiceDefaults.Http;

public static class RouteHandlerBuilderExtensions
{
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder)
        where T : class =>
        builder.AddEndpointFilter<ValidationFilter<T>>().ProducesValidationProblem();
}
