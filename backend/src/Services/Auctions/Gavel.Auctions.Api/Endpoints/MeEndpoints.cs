using Gavel.Auctions.Application.Auctions;
using Gavel.Auctions.Application.Common;
using Gavel.Auctions.Application.Me;
using Gavel.ServiceDefaults.Endpoints;
using Gavel.ServiceDefaults.Http;

namespace Gavel.Auctions.Api.Endpoints;

internal sealed class MeEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/me").WithTags("My activity");

        group.MapGet("/bids", async ([AsParameters] ListMyBids.Query query, ListMyBids.Handler handler, CancellationToken ct) =>
                TypedResults.Ok(await handler.HandleAsync(query, ct)))
            .WithValidation<ListMyBids.Query>()
            .Produces<PagedResult<MyBidDto>>();

        group.MapGet("/auctions", async ([AsParameters] ListMyAuctions.Query query, ListMyAuctions.Handler handler, CancellationToken ct) =>
                TypedResults.Ok(await handler.HandleAsync(query, ct)))
            .WithValidation<ListMyAuctions.Query>()
            .Produces<PagedResult<AuctionSummaryDto>>();

        group.MapGet("/watchlist", async ([AsParameters] ListWatchlist.Query query, ListWatchlist.Handler handler, CancellationToken ct) =>
                TypedResults.Ok(await handler.HandleAsync(query, ct)))
            .WithValidation<ListWatchlist.Query>()
            .Produces<PagedResult<AuctionSummaryDto>>();
    }
}
