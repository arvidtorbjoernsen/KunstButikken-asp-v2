using System;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

using KunstButikken.AuctionService.Domain.Interfaces;
using KunstButikken.AuctionService.Domain.Models;
using KunstButikken.Common.Logging;
using KunstButikken.ServiceDefaults;
using KunstButikken.AuctionService.Infrastructure.Persistence;

namespace KunstButikken.AuctionService.Application.Services;

public class AuctionSeeder(
    ILogger<AuctionSeeder> logger,
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IDateTimeProvider clock,
    IHttpClientFactory httpFactory)
    : IAuctionSeeder
{
    public async Task ApplyMigrationsAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        try
        {
            await db.Database.MigrateAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogMessages.Warning_Msg_21(logger, ex);
        }
    }

    public async Task SeedAuctionsIfEmptyAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();

        bool shouldSeed;
        try
        {
            shouldSeed = !await db.Auctions.AnyAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogMessages.Error_Msg_22(logger, ex);
            return;
        }

        if (!shouldSeed)
        {
            LogMessages.Information_Msg_23(logger, null);
            return;
        }

        var artServiceUrl = configuration["ART_SERVICE_URL"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(artServiceUrl))
        {
            LogMessages.Warning_Msg_24(logger, null);
            return;
        }

        var maxAttempts = 10;
        var delay = TimeSpan.FromSeconds(2);
        List<ArtDto>? arts = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var http = httpFactory.CreateClient();
                if (http.BaseAddress == null)
                {
                    http.BaseAddress = new Uri(artServiceUrl);
                }

                var artUri = http.BaseAddress != null
                    ? new Uri(http.BaseAddress, "/api/art")
                    : new Uri(artServiceUrl + "/api/art");
                using var resp = await http.GetAsync(artUri, ct).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "[AuctionSeeding] Attempt {Attempt}/{MaxAttempts}: Failed to fetch art from {Url} (status {Status}). Retrying in {Delay}s...",
                        attempt, maxAttempts, artServiceUrl, (int)resp.StatusCode, delay.TotalSeconds);
                }
                else
                {
                    arts = await resp.Content.ReadFromJsonAsync<List<ArtDto>>(ct).ConfigureAwait(false);
                    if (arts is { Count: > 0 })
                    {
                        break;
                    }

                    LogMessages.Warning_Attempt_MaxAttempts_Delay_25(logger, attempt, maxAttempts, delay.TotalSeconds, null);
                }
            }
            catch (Exception ex)
            {
                LogMessages.Warning_Attempt_MaxAttempts_Delay_26(logger, attempt, maxAttempts, delay.TotalSeconds, ex);
            }

            await Task.Delay(delay, ct).ConfigureAwait(false);

            // Maybe auctions were seeded elsewhere while we waited
            if (await db.Auctions.AnyAsync(ct).ConfigureAwait(false))
            {
                LogMessages.Information_Msg_27(logger, null);
                return;
            }
        }

        if (arts is null || arts.Count == 0)
        {
            LogMessages.Warning_Msg_28(logger, null);
            return;
        }

        // Use cryptographically secure RNG to pick items for seeding to satisfy analyzer CA5394.
        var selection = arts.OrderBy(_ => GetRandomInt()).Take(5).ToList();
        var now = clock.UtcNow;

        foreach (var a in selection)
        {
            var starts = now.AddMinutes(-GetRandomInt(0, 60));
            var ends = now.AddMinutes(GetRandomInt(30, 240));
            var sellerName = a.SellerDisplayName;
            if (string.IsNullOrWhiteSpace(sellerName))
            {
                sellerName = a.SellerId switch
                {
                    var s when s == Guid.Parse("11111111-1111-1111-1111-111111111111") => "Seller One",
                    var s when s == Guid.Parse("22222222-2222-2222-2222-222222222222") => "Seller Two",
                    var s when s == Guid.Parse("33333333-3333-3333-3333-333333333333") => "Seller Three",
                    _ => "Seed Seller"
                };
            }

            db.Auctions.Add(new Auction
            {
                Id = Guid.NewGuid(),
                ArtId = a.Id,
                SellerId = a.SellerId ?? Guid.Empty,
                SellerDisplayName = sellerName,
                StartsAt = starts,
                EndsAt = ends,
                StartingPrice = Math.Max(a.Price, 10),
                ReservePrice = Math.Round(Math.Max(a.Price, 10) * 1.2m, 2),
                Status = AuctionStatus.Open
            });
        }

        // SaveChangesAsync returns number of state entries written but we don't need the value here
        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);
        LogMessages.Information_Count_29(logger, selection.Count, null);
    }

    private static int GetRandomInt()
    {
        // Return a non-negative random int using RNG
        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        var val = BitConverter.ToUInt32(bytes, 0) & int.MaxValue;
        return (int)val;
    }

    private static int GetRandomInt(int minValue, int maxValue)
    {
        if (minValue >= maxValue)
        {
            return minValue;
        }

        var range = (long)maxValue - minValue;
        // Use RandomNumberGenerator to produce unbiased random in range
        var uintRange = (ulong)range;
        while (true)
        {
            Span<byte> bytes = stackalloc byte[8];
            RandomNumberGenerator.Fill(bytes);
            var sample = BitConverter.ToUInt64(bytes);
            var result = sample % uintRange;
            return (int)(minValue + (long)result);
        }
    }

    private sealed class ArtDto
    {
        public Guid Id { get; set; }
        public decimal Price { get; set; }
        public Guid? SellerId { get; set; }
        public string? SellerDisplayName { get; set; }
    }
}
