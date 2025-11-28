using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using KunstButikken.ArtService.Application.Interfaces;
using KunstButikken.ArtService.Domain.Interfaces;
using KunstButikken.ArtService.Domain.Models;
using KunstButikken.ArtService.Infrastructure.Data;
using KunstButikken.Common.Logging;
using KunstButikken.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace KunstButikken.ArtService.Application.Services;

public class ArtSeeder : IArtSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<ArtSeeder> _logger;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly IDateTimeProvider _clock;
    private readonly IHttpClientFactory _httpClientFactory;

    public ArtSeeder(
        ILogger<ArtSeeder> logger,
        IServiceProvider services,
        IConfiguration config,
        IHostEnvironment env,
        IDateTimeProvider clock,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _services = services;
        _config = config;
        _env = env;
        _clock = clock;
        _httpClientFactory = httpClientFactory;
    }

    public async Task EnsureDatabaseMigratedAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArtDbContext>();
        await EnsureDatabaseMigratedInternalAsync(db, cancellationToken).ConfigureAwait(false);
    }

    public async Task SeedArtAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArtDbContext>();
        await EnsureDatabaseMigratedInternalAsync(db, cancellationToken).ConfigureAwait(false);

        var shouldSeed = false;
        try
        {
            shouldSeed = !await db.Arts.AnyAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException)
        {
            LogMessages.Warning_Msg_34(_logger, ex);
            shouldSeed = true;
        }
        catch (Exception ex)
        {
            LogMessages.Error_Msg_35(_logger, ex);
            return;
        }

        if (!shouldSeed)
        {
            LogMessages.Information_Msg_36(_logger, null);
            return;
        }

        LogMessages.Information_Msg_37(_logger, null);

        try
        {
            var requiredColumnCount = 2;
            var conn = db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                @"SELECT count(*) FROM information_schema.columns WHERE lower(table_name)='arts' AND lower(column_name) IN ('artist','sellerdisplayname')";
            var countObj = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            var found = Convert.ToInt32(countObj ?? 0, CultureInfo.InvariantCulture);

            if (found < requiredColumnCount)
            {
                _logger.LogWarning(
                    "[ArtSeeding] Skipping seeding because Arts table is missing expected columns (found {Found}/{Required}).",
                    found, requiredColumnCount);
                return;
            }
        }
        catch (Exception ex)
        {
            LogMessages.Warning_Msg_38(_logger, ex);
        }

        var sellers = await FetchSellersAsync(cancellationToken).ConfigureAwait(false);
        if (sellers == null || sellers.Length == 0)
        {
            LogMessages.Warning_Msg_39(_logger, null);
            sellers = new[]
            {
                new SellerInfo(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Seller One"),
                new SellerInfo(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Seller Two"),
                new SellerInfo(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Seller Three")
            };
        }
        else
        {
            LogMessages.Information_Count_40(_logger, sellers.Length, null);
        }

        var seedDir = Path.Combine(_env.ContentRootPath ?? AppContext.BaseDirectory, "SeedImages");
        var metaPath = Path.Combine(seedDir, "art_metadata.json");

        var metas = new List<SeedMeta>();
        if (File.Exists(metaPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(metaPath, cancellationToken).ConfigureAwait(false);
                metas = JsonSerializer.Deserialize<List<SeedMeta>>(json, JsonOptions) ?? new List<SeedMeta>();
            }
            catch (Exception ex)
            {
                LogMessages.Error_Msg_41(_logger, ex);
            }
        }

        var validMetas = metas
            .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Filename))
            .Select(m => new { Meta = m, Path = Path.Combine(seedDir, m.Filename) })
            .Where(x => File.Exists(x.Path))
            .Select(x => x.Meta)
            .Take(10)
            .ToList();

        var arts = new List<Art>();
        static string GetContentType(string path) => Path.GetExtension(path).ToUpperInvariant() switch
        {
            ".PNG" => "image/png",
            ".JPG" => "image/jpeg",
            ".JPEG" => "image/jpeg",
            ".SVG" => "image/svg+xml",
            ".GIF" => "image/gif",
            _ => "application/octet-stream"
        };

        for (var i = 0; i < validMetas.Count; i++)
        {
            var meta = validMetas[i];
            var sellerInfo = sellers[i % sellers.Length];
            var imageUrl = "https://via.placeholder.com/600x400?text=Art";

            if (!string.IsNullOrWhiteSpace(meta.Filename))
            {
                var filePath = Path.Combine(seedDir, meta.Filename);
                if (File.Exists(filePath))
                {
                    try
                    {
                        var blob = scope.ServiceProvider.GetService<IBlobStorage>();
                        if (blob != null)
                        {
                            await using var fs = File.OpenRead(filePath);
                            var blobName = $"seed/{Guid.NewGuid()}{Path.GetExtension(filePath)}";
                            imageUrl = await blob.UploadAsync(blobName, fs, GetContentType(filePath), cancellationToken)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            LogMessages.Warning_Msg_42(_logger, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessages.Error_Filename_43(_logger, meta.Filename, ex);
                    }
                }
            }

            var titleNb = meta.TitleNo ?? $"Seeded Art #{i + 1}";
            var titleEn = meta.TitleEn ?? $"Seeded Art #{i + 1}";
            var descriptionNb = meta.DescriptionNo ?? $"Sample description for art #{i + 1}";
            var descriptionEn = meta.DescriptionEn ?? $"Sample description for art #{i + 1}";

            arts.Add(new Art
            {
                Id = Guid.NewGuid(),
                TitleNb = titleNb,
                TitleEn = titleEn,
                DescriptionNb = descriptionNb,
                DescriptionEn = descriptionEn,
                ImageUrl = new Uri(imageUrl),
                Price = Math.Round((decimal)(RandomNumberGenerator.GetInt32(900) + 100), 2),
                SellerId = sellerInfo.UserId,
                Artist = $"Artist {i + 1}",
                SellerDisplayName = sellerInfo.DisplayName,
                Status = ArtStatus.Published,
                IsVerified = true,
                IsFeatured = (i + 1) % 5 == 0,
                CreatedAt = _clock.UtcNow.AddMinutes(-(i + 1))
            });
        }

        await db.Arts.AddRangeAsync(arts, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogMessages.Information_Count_44(_logger, arts.Count, null);
    }

    private async Task EnsureDatabaseMigratedInternalAsync(ArtDbContext db, CancellationToken cancellationToken)
    {
        if (!db.Database.IsRelational())
        {
            await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        LogMessages.Information_Msg_45(_logger, null);
        var attempts = 0;
        var maxAttempts = 10;
        var delay = TimeSpan.FromSeconds(2);

        while (true)
        {
            try
            {
                var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);
                if (pending.Any())
                {
                    LogMessages.Information_Msg_46(_logger, null);
                    await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
                    LogMessages.Information_Msg_47(_logger, null);
                }
                else
                {
                    LogMessages.Information_Msg_48(_logger, null);
                    await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
                }
                break;
            }
            catch (PostgresException pex) when (pex.SqlState == "42P01" && attempts < maxAttempts)
            {
                attempts++;
                _logger.LogWarning(
                    "[ArtSeeding] Postgres error 42P01 during migration attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}s...",
                    attempts, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (attempts < maxAttempts)
            {
                attempts++;
                _logger.LogWarning(ex,
                    "[ArtSeeding] Migration failed (attempt {Attempt}/{Max}). Retrying in {Delay}s...",
                    attempts, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogMessages.Error_Message_49(_logger, ex.Message, ex);
                throw;
            }
        }
    }

    private async Task<SellerInfo[]?> FetchSellersAsync(CancellationToken cancellationToken)
    {
        var userServiceUrl = _config["USER_SERVICE_URL"] ?? _config["NEXT_PUBLIC_USER_SERVICE_URL"];
        if (string.IsNullOrWhiteSpace(userServiceUrl))
        {
            LogMessages.Warning_Msg_50(_logger, null);
            return null;
        }

        var maxAttempts = 5;
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var http = _httpClientFactory.CreateClient();
                http.BaseAddress ??= new Uri(userServiceUrl.TrimEnd('/'));
                var sellersUri = new Uri(http.BaseAddress, "/api/sellers");
                using var resp = await http.GetAsync(sellersUri, cancellationToken).ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[ArtSeeding] Attempt {Attempt}/{Max}: Failed to fetch sellers (status {Status}). Retrying in {Delay}s",
                        attempt, maxAttempts, (int)resp.StatusCode, delay.TotalSeconds);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var sellers = await resp.Content.ReadFromJsonAsync<List<SellerDto>>(cancellationToken).ConfigureAwait(false);
                if (sellers is { Count: > 0 })
                {
                    return sellers
                        .Take(3)
                        .Select(s => new SellerInfo(s.UserId, s.DisplayName ?? $"Seller {s.UserId}")).ToArray();
                }

                _logger.LogWarning(
                    "[ArtSeeding] Attempt {Attempt}/{Max}: Seller list empty. Retrying in {Delay}s",
                    attempt, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogMessages.Warning_Attempt_MaxAttempts_Delay_52(_logger, attempt, maxAttempts, delay.TotalSeconds, ex);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        LogMessages.Warning_MaxAttempts_54(_logger, maxAttempts, null);
        return null;
    }

    private sealed record SellerInfo(Guid UserId, string DisplayName);

    private sealed class SellerDto
    {
        [JsonPropertyName("userId")] public Guid UserId { get; set; }
        [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    }

    private sealed class SeedMeta
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

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
