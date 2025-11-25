using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

using KunstButikken.ArtService.Data;
using KunstButikken.ArtService.Domain.Models;
using KunstButikken.ArtService.Services;
using KunstButikken.ServiceDefaults;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.ArtService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController(
    ArtDbContext db,
    IBlobStorage blob,
    IWebHostEnvironment env,
    IConfiguration cfg,
    IDateTimeProvider clock,
    IHttpClientFactory httpFactory)
    : ControllerBase
{
    // Reuse JsonSerializerOptions to satisfy CA2000/Json caching suggestions
    private static readonly JsonSerializerOptions SJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IBlobStorage _blob = blob ?? throw new ArgumentNullException(nameof(blob));
    private readonly IConfiguration _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));

    private readonly IDateTimeProvider _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    // validate constructor parameters and store as readonly fields to satisfy analyzers
    private readonly ArtDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly IWebHostEnvironment _env = env ?? throw new ArgumentNullException(nameof(env));

    private readonly IHttpClientFactory
        _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));

    private static string GetContentType(string path)
    {
        var ext = Path.GetExtension(path).ToUpperInvariant();
        return ext switch
        {
            ".PNG" => "image/png",
            ".JPG" => "image/jpeg",
            ".JPEG" => "image/jpeg",
            ".SVG" => "image/svg+xml",
            ".GIF" => "image/gif",
            _ => "application/octet-stream"
        };
    }

    [SuppressMessage("Reliability", "CA3003:Potential file path injection",
        Justification =
            "Seed endpoint is restricted to dev and/or controlled via env var; path override is optional and intended for test environments.")]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "HttpClient instances created from IHttpClientFactory should not be disposed here.")]
    [SuppressMessage("Security", "CA5394:Do not use insecure randomness",
        Justification = "Deterministic, fixed-seed Random is intentional for deterministic seeding in tests/dev only.")]
    [HttpPost]
    public async Task<ActionResult<SeedResult>> Seed([FromBody] SeedRequest? request)
    {
        // Only allow in Development unless explicitly forced via environment variable ALLOW_SEED_ENDPOINT=true
        var allow = _env.IsDevelopment() ||
                    string.Equals(_cfg["ALLOW_SEED_ENDPOINT"], "true", StringComparison.OrdinalIgnoreCase);
        if (!allow)
        {
            return Forbid();
        }

        var result = new SeedResult();

        // Prevent duplicate seeding unless Force=true
        var alreadyHasData = await _db.Arts.AnyAsync().ConfigureAwait(false);
        if (alreadyHasData && request?.Force != true)
        {
            result.Messages.Add("Database already contains art. Use Force=true to seed anyway.");
            return Ok(result);
        }

        // locale resolution retained for future use; currently not consumed
        _ = request?.Locale
            ?? _cfg["Seed:Locale"]
            ?? _cfg["SEED_LOCALE"]
            ?? "en";

        // Determine seed path
        var pathOverride = request?.Path;
        string seedDir;
        if (!string.IsNullOrWhiteSpace(pathOverride))
        {
            seedDir = pathOverride!;
        }
        else
            // Try environment variable first
        {
            seedDir = _cfg["SEED_IMAGES_PATH"]
                      // default to Frontend/SeedImages relative to ArtService content root
                      ?? Path.GetFullPath(Path.Combine(
                          string.IsNullOrWhiteSpace(_env.ContentRootPath)
                              ? AppContext.BaseDirectory
                              : _env.ContentRootPath,
                          "../KunstButikken.Frontend/SeedImages"));
        }

        // Diagnostic logging for tests — Console.WriteLine is safe here; remove broad empty catch
        Console.WriteLine($"DEBUG[SeedController]: request.Path='{request?.Path}'");
        Console.WriteLine($"DEBUG[SeedController]: cfg[SEED_IMAGES_PATH]='{_cfg["SEED_IMAGES_PATH"]}'");
        Console.WriteLine($"DEBUG[SeedController]: seedDir='{seedDir}'");

        if (!Directory.Exists(seedDir))
        {
            result.Messages.Add($"Seed directory not found: {seedDir}");
            return NotFound(result);
        }

        var metaPath = Path.Combine(seedDir, "art_metadata.json");
        if (!System.IO.File.Exists(metaPath))
        {
            result.Messages.Add($"art_metadata.json not found in {seedDir}");
            return NotFound(result);
        }

        List<SeedMeta>? metas;
        try
        {
            var json = await System.IO.File.ReadAllTextAsync(metaPath).ConfigureAwait(false);
            metas = JsonSerializer.Deserialize<List<SeedMeta>>(json, SJsonOptions);
        }
        catch (JsonException ex)
        {
            var bad = new SeedResult();
            bad.Messages.Add($"Failed to parse metadata: {ex.Message}");
            return BadRequest(bad);
        }

        if (metas is null || metas.Count == 0)
        {
            result.Messages.Add("No metadata entries found.");
            return Ok(result);
        }

        var limit = Math.Clamp(request?.Limit ?? 10, 1, 100);
        // Use crypto RNG for seeded prices to remove analyzer warnings and avoid shared Random state.
        // Prices are generated using RandomNumberGenerator.GetInt32 for cents-like variance.
        // Deterministic repeatability is not required for this endpoint.

        // Use deterministic seller placeholders (must exist in UserService for realistic linking; otherwise arbitrary GUIDs)
        var sellerIds = new[]
        {
            Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222"), Guid.Parse("33333333-3333-3333-3333-333333333333")
        };

        var items = metas
            .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Filename))
            .Select(m => new
            {
                Meta = m, Path = Path.Combine(seedDir, m.Filename)
            })
            .Where(x => System.IO.File.Exists(x.Path))
            .Take(limit)
            .ToList();

        foreach (var (meta, filePath) in items.Select(x => (x.Meta, x.Path)))
        {
            var imageUrl = "https://via.placeholder.com/600x400?text=Art";
            try
            {
#pragma warning disable CA2007 // FileStream is awaited-disposed even though OpenRead returns a synchronous stream.
                await using var fs = System.IO.File.OpenRead(filePath);
#pragma warning restore CA2007
                var blobName = $"seed/{Guid.NewGuid()}{Path.GetExtension(filePath)}";
                var uploadedUrl = await _blob.UploadAsync(blobName, fs, GetContentType(filePath)).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(uploadedUrl))
                {
                    imageUrl = uploadedUrl;
                }
            }
            catch (IOException)
            {
                // keep placeholder
            }

            var titleEn = meta.TitleEn ?? "Seeded Art";
            var titleNb = meta.TitleNo ?? meta.TitleEn ?? "Seeded Art";
            var descriptionEn = meta.DescriptionEn ?? "Sample description";
            var descriptionNb = meta.DescriptionNo ?? meta.DescriptionEn ?? "Sample description";

            var idx = result.Inserted + result.Skipped;
            var seller = sellerIds[idx % sellerIds.Length];
            var sellerDisplayName = (idx % sellerIds.Length) switch
            {
                0 => "Seller One",
                1 => "Seller Two",
                _ => "Seller Three"
            };

            _db.Arts.Add(new Art
            {
                Id = Guid.NewGuid(),
                TitleEn = titleEn,
                TitleNb = titleNb,
                DescriptionEn = descriptionEn,
                DescriptionNb = descriptionNb,
                ImageUrl = new Uri(imageUrl),
                Price = Math.Round((decimal)(RandomNumberGenerator.GetInt32(900) + 100), 2),
                SellerId = seller,
                Artist = "Seed Artist",
                SellerDisplayName = sellerDisplayName,
                Status = ArtStatus.Published,
                IsVerified = true,
                IsFeatured = (idx + 1) % 5 == 0,
                CreatedAt = _clock.UtcNow
            });
            result.Inserted++;
        }

        await _db.SaveChangesAsync().ConfigureAwait(false);
        result.Messages.Add($"Seeded {result.Inserted} items from {seedDir}");

        // Optionally seed auctions for 5 random artworks
        try
        {
            var auctionsUrl = _cfg["AUCTIONS_SERVICE_URL"];
            if (!string.IsNullOrWhiteSpace(auctionsUrl))
            {
                var baseUri = new Uri(auctionsUrl.TrimEnd('/'));
                using var http = _httpFactory.CreateClient();
                if (http.BaseAddress == null)
                {
                    http.BaseAddress = baseUri;
                }

                using var resp = await http.PostAsJsonAsync("/api/seed", new
                    {
                        Count = 5, Force = false
                    })
                    .ConfigureAwait(false);
                if (resp.IsSuccessStatusCode)
                {
                    var msg = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    result.Messages.Add($"Auctions seeded: {msg}");
                }
                else
                {
                    result.Messages.Add($"Auction seed failed: {(int)resp.StatusCode}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            result.Messages.Add("Auction seed HTTP error: " + ex.Message);
        }

        return Ok(result);
    }

    private class SeedMeta
    {
        [JsonPropertyName("filename")] public string Filename { get; set; } = string.Empty;

        // Accept both
        [JsonPropertyName("file_name")]
        public string FilenameAlt
        {
            get => Filename;
            set => Filename = value;
        }

        [JsonPropertyName("title_en")] public string? TitleEn { get; set; }
        [JsonPropertyName("title_no")] public string? TitleNo { get; set; }
        [JsonPropertyName("description_en")] public string? DescriptionEn { get; set; }
        [JsonPropertyName("description_no")] public string? DescriptionNo { get; set; }
    }
}

// DTOs used by the SeedController action - public so the controller's public action signature is consistent
public class SeedRequest
{
    public int? Limit { get; set; }
    public string? Locale { get; set; }

    public bool Force { get; set; }

    // Optional explicit path override
    public string? Path { get; set; }
}

public class SeedResult
{
    public int Inserted { get; set; }

    public int Skipped { get; set; }

    // expose as a getter-only list: serializable by System.Text.Json and prevents external replacement
    public Collection<string> Messages { get; } = new();
}
