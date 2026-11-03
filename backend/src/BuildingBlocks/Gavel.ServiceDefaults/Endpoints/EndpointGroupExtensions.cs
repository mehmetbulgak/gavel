using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace Gavel.ServiceDefaults.Endpoints;

public static class EndpointGroupExtensions
{
    public static IEndpointRouteBuilder MapEndpointGroups(this IEndpointRouteBuilder app, Assembly assembly)
    {
        var groups = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IEndpointGroup).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .Select(type => (IEndpointGroup)Activator.CreateInstance(type)!);

        foreach (var group in groups)
        {
            group.Map(app);
        }

        return app;
    }
}
