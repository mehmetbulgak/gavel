using Microsoft.AspNetCore.Routing;

namespace Gavel.ServiceDefaults.Endpoints;

public interface IEndpointGroup
{
    void Map(IEndpointRouteBuilder app);
}
