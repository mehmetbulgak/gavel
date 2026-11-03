using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Gavel.ServiceDefaults.Http;

internal sealed class ValidationFilter<T> : IEndpointFilter
    where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        var argument = context.Arguments.OfType<T>().FirstOrDefault();

        if (validator is not null && argument is not null)
        {
            var validation = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
            if (!validation.IsValid)
            {
                return TypedResults.ValidationProblem(validation.ToDictionary());
            }
        }

        return await next(context);
    }
}
