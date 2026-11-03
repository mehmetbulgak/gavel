using Gavel.Auctions.Application.Auctions;
using Gavel.Auctions.Application.Common;
using Gavel.ServiceDefaults.Endpoints;
using Gavel.ServiceDefaults.Http;

namespace Gavel.Auctions.Api.Endpoints;

internal sealed class AuctionEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auctions").WithTags("Auctions");

        group.MapGet(string.Empty, ListAsync)
            .AllowAnonymous()
            .WithValidation<ListAuctions.Query>()
            .Produces<PagedResult<AuctionSummaryDto>>();

        group.MapGet("/stats", StatsAsync)
            .AllowAnonymous()
            .Produces<MarketStatsDto>();

        group.MapGet("/rules", () => TypedResults.Ok(AuctionRulesDto.Current))
            .AllowAnonymous();

        group.MapGet("/{id:guid}", GetAsync)
            .AllowAnonymous()
            .Produces<AuctionDetailsDto>();

        group.MapGet("/{id:guid}/bids", BidHistoryAsync)
            .AllowAnonymous()
            .WithValidation<GetBidHistory.Query>()
            .Produces<PagedResult<BidDto>>();

        group.MapPost(string.Empty, CreateAsync)
            .WithValidation<CreateAuction.Command>()
            .Produces<AuctionSummaryDto>(StatusCodes.Status201Created);

        group.MapPost("/{id:guid}/bids", PlaceBidAsync)
            .WithValidation<PlaceBid.Request>()
            .Produces<BidResultDto>();

        group.MapPost("/{id:guid}/cancel", CancelAsync)
            .Produces(StatusCodes.Status204NoContent);

        group.MapPut("/{id:guid}/watch", WatchAsync)
            .Produces(StatusCodes.Status204NoContent);

        group.MapDelete("/{id:guid}/watch", UnwatchAsync)
            .Produces(StatusCodes.Status204NoContent);
    }

    private static async Task<IResult> ListAsync([AsParameters] ListAuctions.Query query, ListAuctions.Handler handler, CancellationToken ct) =>
        TypedResults.Ok(await handler.HandleAsync(query, ct));

    private static async Task<IResult> StatsAsync(GetMarketStats.Handler handler, CancellationToken ct) =>
        TypedResults.Ok(await handler.HandleAsync(ct));

    private static async Task<IResult> GetAsync(Guid id, GetAuction.Handler handler, CancellationToken ct) =>
        (await handler.HandleAsync(id, ct)).ToHttpResult();

    private static async Task<IResult> BidHistoryAsync(
        Guid id,
        [AsParameters] GetBidHistory.Query query,
        GetBidHistory.Handler handler,
        CancellationToken ct) =>
        (await handler.HandleAsync(id, query, ct)).ToHttpResult();

    private static async Task<IResult> CreateAsync(CreateAuction.Command command, CreateAuction.Handler handler, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct)).ToHttpResult(auction => TypedResults.Created($"/api/auctions/{auction.Id}", auction));

    private static async Task<IResult> PlaceBidAsync(Guid id, PlaceBid.Request request, PlaceBid.Handler handler, CancellationToken ct) =>
        (await handler.HandleAsync(id, request, ct)).ToHttpResult();

    private static async Task<IResult> CancelAsync(Guid id, CancelAuction.Handler handler, CancellationToken ct) =>
        (await handler.HandleAsync(id, ct)).ToHttpResult();

    private static async Task<IResult> WatchAsync(Guid id, WatchAuction.Handler handler, CancellationToken ct) =>
        (await handler.WatchAsync(id, ct)).ToHttpResult();

    private static async Task<IResult> UnwatchAsync(Guid id, WatchAuction.Handler handler, CancellationToken ct) =>
        (await handler.UnwatchAsync(id, ct)).ToHttpResult();
}
