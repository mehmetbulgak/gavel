using System.Net;
using System.Security.Cryptography;
using Gavel.Simulator.Api;
using Microsoft.Extensions.Options;

namespace Gavel.Simulator;

internal sealed partial class SimulationWorker(
    GavelApi api,
    IOptions<SimulatorOptions> options,
    TimeProvider clock,
    ILogger<SimulationWorker> logger) : BackgroundService
{
    private static readonly string[] BotNames = ["Mert", "Elif", "Can", "Zeynep", "Kerem", "Defne", "Ozan", "Selin", "Arda", "Ece", "Baran", "Lara"];
    private static readonly TimeSpan ListingCheckInterval = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan RateLimitPause = TimeSpan.FromSeconds(30);

    private readonly Dictionary<(string Bot, Guid Auction), decimal> _valuations = [];
    private DateTimeOffset _nextListingCheck = DateTimeOffset.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var (bots, demo) = CreateAccounts(settings);

        await WaitForGatewayAsync(stoppingToken);
        LogStarted(logger, bots.Count, settings.GatewayUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await KeepMarketStockedAsync(bots, demo, stoppingToken);
                await PlaceBotBidAsync(bots, stoppingToken);
            }
            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(RateLimitPause, clock, stoppingToken);
            }
            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Unauthorized)
            {
                bots.ForEach(bot => bot.Forget());
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogStepFailed(logger, exception);
            }

            var pause = Random.Shared.Next(settings.MinBidIntervalMilliseconds, settings.MaxBidIntervalMilliseconds);
            await Task.Delay(TimeSpan.FromMilliseconds(pause), clock, stoppingToken);
        }
    }

    private static (List<Bot> Bots, Bot Demo) CreateAccounts(SimulatorOptions settings)
    {
        var configuredPassword = settings.BotPassword;
        var password = string.IsNullOrWhiteSpace(configuredPassword)
            ? $"{Convert.ToBase64String(RandomNumberGenerator.GetBytes(18))}a1"
            : configuredPassword;
        var run = string.IsNullOrWhiteSpace(configuredPassword) ? $".{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(3))}" : string.Empty;

        var bots = Enumerable.Range(0, settings.BotCount)
            .Select(i => new Bot($"bot{i + 1}{run}@bots.gavel.test", BotNames[i % BotNames.Length], password, isSeller: i < 3))
            .ToList();
        var demo = new Bot(settings.DemoEmail, settings.DemoDisplayName, settings.DemoPassword, isSeller: true);
        return (bots, demo);
    }

    private async Task WaitForGatewayAsync(CancellationToken ct)
    {
        while (true)
        {
            try
            {
                await api.LiveAuctionsAsync(ct);
                return;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogWaiting(logger);
                await Task.Delay(TimeSpan.FromSeconds(3), clock, ct);
            }
        }
    }

    private async Task KeepMarketStockedAsync(List<Bot> bots, Bot demo, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        if (now < _nextListingCheck)
        {
            return;
        }

        _nextListingCheck = now.Add(ListingCheckInterval);

        var live = await api.LiveAuctionsAsync(ct);
        var takenTitles = live.Items.Select(a => a.Title).ToHashSet(StringComparer.Ordinal);

        var demoToken = await demo.TokenAsync(api, clock, ct);
        var demoListings = await api.MyAuctionsAsync(demoToken, ct);
        if (!demoListings.Items.Any(a => a.Status is "Live" or "Scheduled"))
        {
            await api.CreateAuctionAsync(NextListing(takenTitles, durationMinutes: 30), demoToken, ct);
        }

        var missing = Math.Min(3, options.Value.TargetLiveAuctions - live.TotalCount);
        var sellers = bots.Where(b => b.IsSeller).ToList();

        for (var i = 0; i < missing; i++)
        {
            var seller = sellers[Random.Shared.Next(sellers.Count)];
            await api.CreateAuctionAsync(NextListing(takenTitles), await seller.TokenAsync(api, clock, ct), ct);
        }

        var liveIds = live.Items.Select(a => a.Id).ToHashSet();
        foreach (var key in _valuations.Keys.Where(k => !liveIds.Contains(k.Auction)).ToList())
        {
            _valuations.Remove(key);
        }
    }

    private async Task PlaceBotBidAsync(List<Bot> bots, CancellationToken ct)
    {
        var live = await api.LiveAuctionsAsync(ct);
        if (live.Items.Count == 0)
        {
            return;
        }

        var pool = Random.Shared.NextDouble() < 0.7 ? live.Items.Take(6).ToList() : live.Items.ToList();
        var auction = pool[Random.Shared.Next(pool.Count)];
        var candidates = bots.Where(b => b.DisplayName != auction.SellerName).ToList();
        var bot = candidates[Random.Shared.Next(candidates.Count)];

        var token = await bot.TokenAsync(api, clock, ct);
        var details = await api.AuctionAsync(auction.Id, token, ct);
        if (details.Viewer is { IsLeading: true } or { IsSeller: true })
        {
            return;
        }

        var valuation = ValuationFor(bot, auction);
        if (details.MinimumNextBid > valuation)
        {
            return;
        }

        var steps = Random.Shared.Next(0, 4);
        var maximum = Math.Min(valuation, decimal.Round(details.MinimumNextBid * (1 + (steps * 0.03m)), 0, MidpointRounding.AwayFromZero));
        await api.BidAsync(auction.Id, Math.Max(maximum, details.MinimumNextBid), token, ct);
    }

    private decimal ValuationFor(Bot bot, AuctionSummary auction)
    {
        var key = (bot.Email, auction.Id);
        if (!_valuations.TryGetValue(key, out var valuation))
        {
            var factor = 1.05m + (decimal)(Random.Shared.NextDouble() * 0.9);
            valuation = decimal.Round(auction.CurrentPrice * factor, 0);
            _valuations[key] = valuation;
        }

        return valuation;
    }

    private static NewAuction NextListing(HashSet<string> takenTitles, int? durationMinutes = null)
    {
        var available = Catalog.Items.Where(i => !takenTitles.Contains(i.Title)).ToList();
        var pool = available.Count > 0 ? available : Catalog.Items;
        var item = pool[Random.Shared.Next(pool.Count)];
        takenTitles.Add(item.Title);
        var duration = durationMinutes ?? Catalog.Durations[Random.Shared.Next(Catalog.Durations.Count)];
        return new NewAuction(item.Title, item.Description, item.Category, item.StartingPrice, item.ReservePrice, null, duration);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Simulation started with {BotCount} bots against {Gateway}")]
    private static partial void LogStarted(ILogger logger, int botCount, string gateway);

    [LoggerMessage(Level = LogLevel.Information, Message = "Waiting for the gateway to come up")]
    private static partial void LogWaiting(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "A simulation step failed; continuing")]
    private static partial void LogStepFailed(ILogger logger, Exception exception);
}
