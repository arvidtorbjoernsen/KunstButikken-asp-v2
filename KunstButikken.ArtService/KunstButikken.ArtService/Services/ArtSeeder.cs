using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

using KunstButikken.ArtService.Data;
using KunstButikken.ArtService.Domain.Models;
using KunstButikken.Common.Logging;
using KunstButikken.ServiceDefaults;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace KunstButikken.ArtService.Services;

public class ArtSeeder(
    ILogger<ArtSeeder> logger,
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IHostEnvironment env,
    IDateTimeProvider clock,
    IHttpClientFactory httpFactory)
    : IArtSeeder
{
    // Reused JsonSerializerOptions for the class to avoid allocating per-call and to avoid invalid local 'static readonly' usage
    private static readonly JsonSerializerOptions SJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Seeding tolerates failures; errors are logged and seeding continues.")]
    public async Task SeedArtAsync(CancellationToken cancellationToken)
    {
        // Reuse serializer options from class field
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArtDbContext>();

        // Ensure database is migrated/created before seeding
        await EnsureDatabaseMigratedAsync(db, cancellationToken).ConfigureAwait(false);

        var shouldSeed = false;
        try
        {
            shouldSeed = !await db.Arts.AnyAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException)
        {
            // If table somehow still doesn't exist, treat as empty for seeding purposes
            LogMessages.Warning_Msg_34(logger, ex);
            shouldSeed = true;
        }
        catch (Exception ex)
        {
            LogMessages.Error_Msg_35(logger, ex);
            return;
        }

        if (!shouldSeed)
        {
            LogMessages.Information_Msg_36(logger, null);
            return;
        }

        LogMessages.Information_Msg_37(logger, null);

        try
        {
            // Ensure the required columns are present in the existing Arts table schema before seeding
            var requiredColumnCount = 2; // artist, sellerdisplayname
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
                logger.LogWarning(
                    "[ArtSeeding] Skipping seeding because Arts table is missing expected columns (found {Found}/{Required}). Verify migrations or schema before seeding.",
                    found, requiredColumnCount);
                return;
            }
        }
        catch (Exception ex)
        {
            LogMessages.Warning_Msg_38(logger, ex);
        }

        // Try to fetch actual sellers from UserService API
        var sellers = await FetchSellersFromUserServiceAsync(cancellationToken).ConfigureAwait(false);

        // Fallback to deterministic IDs if UserService is not available
        if (sellers == null || sellers.Length == 0)
        {
            LogMessages.Warning_Msg_39(logger, null);
            sellers = new[]
            {
                new SellerInfo(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Seller One"), new SellerInfo(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Seller Two"),
                new SellerInfo(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Seller Three")
            };
        }
        else
        {
            LogMessages.Information_Count_40(logger, sellers.Length, null);
        }

        // Read metadata file from SeedImages
        var seedDir = Path.Combine(env.ContentRootPath ?? AppContext.BaseDirectory, "SeedImages");
        var metaPath = Path.Combine(seedDir, "art_metadata.json");

        List<SeedMeta> metas = new();
        if (File.Exists(metaPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(metaPath, cancellationToken).ConfigureAwait(false);
                metas = JsonSerializer.Deserialize<List<SeedMeta>>(json, SJsonOptions) ?? new List<SeedMeta>();
            }
            catch (Exception ex)
            {
                LogMessages.Error_Msg_41(logger, ex);
                metas = new List<SeedMeta>();
            }
        }

        // Select up to 10 metadata items that have corresponding image files
        var validMetas = metas
            .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Filename))
            .Select(m => new
            {
                Meta = m, Path = Path.Combine(seedDir, m.Filename)
            })
            .Where(x => File.Exists(x.Path))
            .Select(x => x.Meta)
            .Take(10)
            .ToList();

        var totalToSeed = validMetas.Count;

        var arts = new List<Art>();
        // Use cryptographically secure randomness (not security-sensitive, but removes analyzer warnings).
        // We produce prices as deterministic-ish cents using RNG to get a uniform distribution of cents.
        // This intentionally drops deterministic seed to satisfy CA5394; deterministic seeding
        // can be re-introduced with a test-only switch if needed.
        // No RNG instance needed — we'll use RandomNumberGenerator.GetInt32 inline when creating prices.

        static string GetContentType(string path)
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

        for (var i = 0; i < totalToSeed; i++)
        {
            var meta = validMetas[i];

            // Assign seller in round-robin across the available sellers
            var sellerInfo = sellers[i % sellers.Length];
            var seller = sellerInfo.UserId;
            var sellerDisplayName = sellerInfo.DisplayName;

            // Default placeholder
            var imageUrl = "https://via.placeholder.com/600x400?text=Art";

            if (meta != null && !string.IsNullOrWhiteSpace(meta.Filename))
            {
                var filePath = Path.Combine(seedDir, meta.Filename);
                if (File.Exists(filePath))
                {
                    try
                    {
                        var blob = scope.ServiceProvider.GetService<IBlobStorage>();
                        if (blob != null)
                        {
#pragma warning disable CA2007 // FileStream is awaited-disposed even though OpenRead returns a synchronous stream.
                            await using var fs = File.OpenRead(filePath);
#pragma warning restore CA2007
                            var blobName = $"seed/{Guid.NewGuid()}{Path.GetExtension(filePath)}";
                            imageUrl = await blob.UploadAsync(blobName, fs, GetContentType(filePath), cancellationToken)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            LogMessages.Warning_Msg_42(logger, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessages.Error_Filename_43(logger, meta.Filename, ex);
                        // ignore and keep placeholder
                    }
                }
            }

            // Populate the multilingual fields
            var titleNb = meta?.TitleNo ?? $"Seeded Art #{i + 1}";
            var titleEn = meta?.TitleEn ?? $"Seeded Art #{i + 1}";
            var descriptionNb = meta?.DescriptionNo ?? $"Sample description for art #{i + 1}";
            var descriptionEn = meta?.DescriptionEn ?? $"Sample description for art #{i + 1}";

            arts.Add(new Art
            {
                Id = Guid.NewGuid(),
                TitleNb = titleNb,
                TitleEn = titleEn,
                DescriptionNb = descriptionNb,
                DescriptionEn = descriptionEn,
                ImageUrl = new Uri(imageUrl),
                Price = Math.Round((decimal)(RandomNumberGenerator.GetInt32(900) + 100), 2),
                SellerId = seller,
                Artist = $"Artist {i + 1}",
                SellerDisplayName = sellerDisplayName,
                Status = ArtStatus.Published,
                IsVerified = true,
                IsFeatured = (i + 1) % 5 == 0,
                CreatedAt = clock.UtcNow.AddMinutes(-(i + 1))
            });
        }

        await db.Arts.AddRangeAsync(arts, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogMessages.Information_Count_44(logger, arts.Count, null);
    }

    public async Task EnsureDatabaseMigratedAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArtDbContext>();
        await EnsureDatabaseMigratedAsync(db, cancellationToken).ConfigureAwait(false);
    }

    // Existing DB-specific migration helper
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Retries are intended; log and retry on various failures.")]
    public async Task EnsureDatabaseMigratedAsync(ArtDbContext db, CancellationToken cancellationToken)
    {
        if (db is null)
        {
            throw new ArgumentNullException(nameof(db));
        }

        if (!db.Database.IsRelational())
        {
            await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        LogMessages.Information_Msg_45(logger, null);
        var attempts = 0;
        var maxAttempts = 10;
        var delay = TimeSpan.FromSeconds(2);

        while (true)
        {
            try
            {
                var pendingMigrations =
                    await db.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);
                if (pendingMigrations.Any())
                {
                    LogMessages.Information_Msg_46(logger, null);
                    await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
                    LogMessages.Information_Msg_47(logger, null);
                }
                else
                {
                    LogMessages.Information_Msg_48(logger, null);
                    await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
                }

                break; // Success
            }
            catch (PostgresException pex) when (pex.SqlState == "42P01" && attempts < maxAttempts)
            {
                attempts++;
                logger.LogWarning(
                    "[ArtSeeding] Postgres error (42P01 - undefined_table) during migration attempt {Attempt}/{MaxAttempts}. Retrying in {Delay} seconds. Message: {Message}",
                    attempts, maxAttempts, delay.TotalSeconds, pex.MessageText);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (attempts < maxAttempts)
            {
                attempts++;
                logger.LogWarning(ex,
                    "[ArtSeeding] Database migration failed (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay} seconds. Message: {Message}",
                    attempts, maxAttempts, delay.TotalSeconds, ex.Message);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogMessages.Error_Message_49(logger, ex.Message, ex);
                throw; // Re-throw if all retries fail
            }
        }
    }

    private async Task<SellerInfo[]?> FetchSellersFromUserServiceAsync(CancellationToken cancellationToken)
    {
        var userServiceUrl = configuration["USER_SERVICE_URL"] ?? configuration["NEXT_PUBLIC_USER_SERVICE_URL"];
        if (string.IsNullOrWhiteSpace(userServiceUrl))
        {
            LogMessages.Warning_Msg_50(logger, null);
            return null;
        }

        var maxAttempts = 5;
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var http = httpFactory.CreateClient();
                if (http.BaseAddress == null)
                {
                    http.BaseAddress = new Uri(userServiceUrl.TrimEnd('/'));
                }

                // Use Uri overload to avoid using string overload
                var sellersUri = http.BaseAddress != null
                    ? new Uri(http.BaseAddress, "/api/sellers")
                    : new Uri(userServiceUrl.TrimEnd('/') + "/api/sellers");

                using var resp = await http.GetAsync(sellersUri, cancellationToken).ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "[ArtSeeding] Attempt {Attempt}/{MaxAttempts}: Failed to fetch sellers from {Url} (status {Status}). Retrying in {Delay}s...",
                        attempt, maxAttempts, userServiceUrl, (int)resp.StatusCode, delay.TotalSeconds);
                }
                else
                {
                    var sellers = await resp.Content.ReadFromJsonAsync<List<SellerDto>>(cancellationToken)
                        .ConfigureAwait(false);
                    if (sellers is { Count: > 0 })
                        // Take up to 3 sellers for seeding and map to SellerInfo
                    {
                        return sellers.Take(3)
                            .Select(s => new SellerInfo(s.UserId,
                                s.DisplayName ?? "Seller " + s.UserId.ToString().AsSpan(0, 8).ToString()))
                            .ToArray();
                    }

                    LogMessages.Warning_Attempt_MaxAttempts_Delay_51(logger, attempt, maxAttempts, delay.TotalSeconds, null);
                }
            }
            catch (HttpRequestException ex)
            {
                LogMessages.Warning_Attempt_MaxAttempts_Delay_52(logger, attempt, maxAttempts, delay.TotalSeconds, ex);
            }
            catch (TaskCanceledException ex)
            {
                LogMessages.Warning_Attempt_MaxAttempts_Delay_53(logger, attempt, maxAttempts, delay.TotalSeconds, ex);
            }

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        LogMessages.Warning_MaxAttempts_54(logger, maxAttempts, null);
        return null;
    }

    // Seed metadata helper - declared at file scope
    private class SeedMeta
    {
        [JsonPropertyName("filename")] public string Filename { get; set; } = string.Empty;

        // Accept both "filename" and "file_name"
        [JsonPropertyName("file_name")]
        public string FilenameAlt
        {
            get => Filename;
            set => Filename = value;
        }

        [JsonPropertyName("title_en")] public string TitleEn { get; } = string.Empty;

        [JsonPropertyName("title_no")] public string TitleNo { get; } = string.Empty;

        [JsonPropertyName("description_en")] public string DescriptionEn { get; } = string.Empty;

        [JsonPropertyName("description_no")] public string DescriptionNo { get; } = string.Empty;
    }

    private sealed class SellerDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
    }

    // Simple typed helper for seeding mapping
    private sealed record SellerInfo(Guid UserId, string DisplayName);
}
