using KunstButikken.AuctionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

using System.Security.Cryptography;

using KunstButikken.AuctionService.Domain.Models;
using KunstButikken.ServiceDefaults;

using Microsoft.AspNetCore.Mvc;

namespace KunstButikken.AuctionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController(
    AuctionDbContext db,
    IConfiguration cfg,
    IWebHostEnvironment env,
    IDateTimeProvider clock,
    IHttpClientFactory httpFactory)
    : ControllerBase
{
    private readonly IDateTimeProvider _clock = clock;
    private readonly AuctionDbContext _db = db;
    private readonly IHttpClientFactory _httpFactory = httpFactory;

    [HttpPost]
    public async Task<ActionResult<SeedResult>> Seed([FromBody] SeedRequest? req)
    {
        // Only allow in Development unless explicitly forced via environment variable ALLOW_SEED_ENDPOINT=true
        var allow = env.IsDevelopment() ||
                    string.Equals(cfg["ALLOW_SEED_ENDPOINT"], "true", StringComparison.OrdinalIgnoreCase);
        if (!allow)
        {
            return Forbid();
        }

        var result = new SeedResult();
        var already = await _db.Auctions.AnyAsync().ConfigureAwait(false);
        if (already && req?.Force != true)
        {
            result.Messages.Add("Auctions already exist. Use Force=true to seed anyway.");
            return Ok(result);
        }

        var artService = cfg["ART_SERVICE_URL"]?.TrimEnd('/') ?? "";
        if (string.IsNullOrWhiteSpace(artService))
        {
            var bad = new SeedResult();
            bad.Messages.Add("ART_SERVICE_URL not configured");
            return BadRequest(bad);
        }

        // IHttpClientFactory produces pooled clients; disposing the wrapper is harmless and satisfies analyzers
        using var http = _httpFactory.CreateClient();
        if (http.BaseAddress == null)
        {
            http.BaseAddress = new Uri(artService);
        }

        // Get published & verified art
        var artUri = http.BaseAddress != null
            ? new Uri(http.BaseAddress, "/api/art")
            : new Uri(artService + "/api/art");
        using var res = await http.GetAsync(artUri).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
        {
            var badStatus = new SeedResult();
            badStatus.Messages.Add($"Failed to fetch art: {(int)res.StatusCode}");
            return StatusCode((int)res.StatusCode, badStatus);
        }

        var arts = await res.Content.ReadFromJsonAsync<List<ArtDto>>().ConfigureAwait(false);
        if (arts is null || arts.Count == 0)
        {
            result.Messages.Add("No art available to create auctions.");
            return Ok(result);
        }

        var count = Math.Clamp(req?.Count ?? 5, 1, Math.Min(arts.Count, 20));

        // Use cryptographic RNG for seeding selection to satisfy CA5394
        var selection = arts.OrderBy(_ => GetRandomInt()).Take(count).ToList();

        foreach (var a in selection)
        {
            var starts = _clock.UtcNow.AddMinutes(-GetRandomInt(0, 60));
            var ends = _clock.UtcNow.AddMinutes(GetRandomInt(30, 240));
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

            _db.Auctions.Add(new Auction
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
            result.Inserted++;
        }

        await _db.SaveChangesAsync().ConfigureAwait(false);
        result.Messages.Add($"Seeded {result.Inserted} auctions from {selection.Count} artworks.");
        return Ok(result);
    }

    private static int GetRandomInt()
    {
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
}
