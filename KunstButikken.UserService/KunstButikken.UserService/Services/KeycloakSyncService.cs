using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using KunstButikken.Common.Logging;
using KunstButikken.ServiceDefaults;
using KunstButikken.UserService.Domain.Entities;
using KunstButikken.UserService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KunstButikken.UserService.Services;

public class KeycloakSyncService(
    ILogger<KeycloakSyncService> logger,
    IHttpClientFactory httpFactory,
    IServiceProvider services,
    IConfiguration config,
    IDateTimeProvider clock,
    IKeycloakAdminClient adminClient)
    : BackgroundService, IKeycloakSyncRunner
{
    // LoggerMessage delegates (non-generic ILogger-based)
    private static readonly Action<ILogger, string, Exception?> _infoMsg =
        Log.Define<string>(LogLevel.Information, new EventId(2200, "InfoMsg"),
            "KeycloakSyncService: {Msg}");

    private static readonly Action<ILogger, string, Exception?> _warningMsg =
        Log.Define<string>(LogLevel.Warning, new EventId(2201, "WarningMsg"),
            "KeycloakSyncService: {Msg}");

    private static readonly Action<ILogger, Exception?> _httpErrorFetchingUsers =
        Log.Define(LogLevel.Error, new EventId(2202, "HttpErrorFetchingUsers"),
            "KeycloakSyncService: HTTP error fetching users after retries");

    private static readonly Action<ILogger, Exception?> _jsonParsingErrorFetchingUsers =
        Log.Define(LogLevel.Error, new EventId(2203, "JsonParsingErrorFetchingUsers"),
            "KeycloakSyncService: JSON parsing error fetching users after retries");

    private static readonly Action<ILogger, Exception?> _dbUpdateErrorUpserting =
        Log.Define(LogLevel.Error, new EventId(2204, "DbUpdateErrorUpserting"),
            "KeycloakSyncService: Database update error upserting users after retries");

    private static readonly Action<ILogger, bool, TimeSpan?, Exception?> _runComplete =
        Log.Define<bool, TimeSpan?>(LogLevel.Information, new EventId(2205, "RunComplete"),
            "KeycloakSyncService: RunOnceAsync complete (succeeded={Succeeded}, duration={Duration})");

    private static readonly Action<ILogger, Exception?> _serviceStopping =
        Log.Define(LogLevel.Information, new EventId(2206, "ServiceStopping"),
            "[KeycloakSync] Service stopping.");

    private static readonly Action<ILogger, Exception?> _initialRunDbFailed =
        Log.Define(LogLevel.Error, new EventId(2207, "InitialRunDbFailed"),
            "[KeycloakSync] Initial run failed catastrophically due to a database update error.");

    private static readonly Action<ILogger, Exception?> _initialRunHttpFailed =
        Log.Define(LogLevel.Error, new EventId(2208, "InitialRunHttpFailed"),
            "[KeycloakSync] Initial run failed catastrophically due to an HTTP request error.");

    private static readonly Action<ILogger, Exception?> _initialRunJsonFailed =
        Log.Define(LogLevel.Error, new EventId(2209, "InitialRunJsonFailed"),
            "[KeycloakSync] Initial run failed catastrophically due to a JSON parsing error.");

    private readonly IKeycloakAdminClient _adminClient =
        adminClient ?? throw new ArgumentNullException(nameof(adminClient));

    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<KeycloakSyncService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly object _statusLock = new();

    public bool IsRunning { get; private set; }
    public DateTimeOffset? LastRunUtc { get; private set; }
    public TimeSpan? LastRunDuration { get; private set; }
    public bool LastRunSucceeded { get; private set; }
    public string? LastRunError { get; private set; }

    public string? LastAuthTokenEndpoint { get; private set; }
    public string? LastAuthTokenRealm { get; private set; }
    public string? LastAuthClientId { get; private set; }
    public string? LastAuthGrant { get; private set; }
    public string? LastAuthHttpError { get; private set; }

    // Admin token acquisition logic has been moved into KeycloakAdminClient

    public async Task TriggerRunAsync(CancellationToken ct = default) => await RunOnceAsync(ct).ConfigureAwait(false);

    public async Task RunOnceAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        lock (_statusLock)
        {
            IsRunning = true;
            LastRunError = null;
        }

        try
        {
            var issuer = config["KEYCLOAK_ISSUER"] ?? config["NEXT_PUBLIC_KEYCLOAK_ISSUER"] ?? string.Empty;
            var realm = config["KEYCLOAK_REALM"] ?? ResolveRealmFromIssuer(issuer) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(realm))
            {
                var msg = "Missing KEYCLOAK_ISSUER or KEYCLOAK_REALM";
                _infoMsg(_logger, msg, null);
                lock (_statusLock)
                {
                    LastRunError = msg;
                }

                return;
            }

            // Resolve admin base with Aspire support
            var adminBase = ResolveAdminBase(issuer, realm);
            if (!string.IsNullOrWhiteSpace(issuer) && issuer.Contains(
                    "aspire.hosting.applicationmodel.endpointreference",
                    StringComparison.OrdinalIgnoreCase))
            {
                var discoverableBase = config["KEYCLOAK_BASE"];
                adminBase = !string.IsNullOrWhiteSpace(discoverableBase)
                    ? discoverableBase.TrimEnd('/')
                    : "http://keycloak";
            }

            if (string.IsNullOrWhiteSpace(adminBase))
            {
                var msg = $"Could not resolve Keycloak admin base URL from issuer {issuer}";
                _warningMsg(_logger, msg, null);
                lock (_statusLock)
                {
                    LastRunError = msg;
                }

                return;
            }

            if (!HasScheme(adminBase))
            {
                adminBase = "http://" + adminBase.TrimStart('/');
            }

            using var http = httpFactory.CreateClient();
            var tokenResult = await _adminClient.TryGetAdminTokenAsync(http, issuer, ct).ConfigureAwait(false);
            LastAuthTokenEndpoint = tokenResult.TokenEndpoint;
            LastAuthTokenRealm = tokenResult.TokenRealm;
            LastAuthClientId = tokenResult.ClientId;
            LastAuthGrant = tokenResult.GrantTried;
            LastAuthHttpError = tokenResult.HttpError;

            if (string.IsNullOrWhiteSpace(tokenResult.Token))
            {
                var msg = "Could not obtain Keycloak admin token";
                lock (_statusLock)
                {
                    LastRunError = msg;
                }

                return;
            }

            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);

            // Fetch users from Keycloak (with retries to tolerate cold-start races)
            var usersUrl = new Uri($"{adminBase}/admin/realms/{realm}/users?max=1000");
            List<KeycloakUserDto> kcUsers = [];
            try
            {
                var text = await RetryAsync(async () =>
                {
                    using var res = await http.GetAsync(usersUrl, ct).ConfigureAwait(false);
                    res.EnsureSuccessStatusCode(); // Throws HttpRequestException for non-success
                    return await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(text))
                {
                    var msg = "Failed to list Keycloak users after retries";
                    _warningMsg(_logger, msg, null);
                    lock (_statusLock)
                    {
                        LastRunError = msg;
                    }

                    return;
                }

                kcUsers = ParseKeycloakUsers(text);
            }
            catch (HttpRequestException ex)
            {
                _httpErrorFetchingUsers(_logger, ex);
                lock (_statusLock)
                {
                    LastRunError = ex.Message;
                }

                return;
            }
            catch (JsonException ex)
            {
                _jsonParsingErrorFetchingUsers(_logger, ex);
                lock (_statusLock)
                {
                    LastRunError = ex.Message;
                }

                return;
            }

            // Upsert into UserDb (extracted to helper to keep RunOnceAsync concise)
            try
            {
                var kcCtx = new KeycloakAdminContext(http, adminBase, realm);
                await RetryAsync(async () =>
                {
                    await UpsertProfilesAsync(kcUsers, kcCtx, ct).ConfigureAwait(false);
                    return true;
                }, ct).ConfigureAwait(false);
            }
            catch (DbUpdateException ex)
            {
                _dbUpdateErrorUpserting(_logger, ex);
                lock (_statusLock)
                {
                    LastRunError = ex.Message;
                }
            }
        }
        finally
        {
            sw.Stop();
            lock (_statusLock)
            {
                LastRunUtc = clock.UtcNow;
                LastRunDuration = sw.Elapsed;
                LastRunSucceeded = string.IsNullOrEmpty(LastRunError);
                IsRunning = false;
            }

            _runComplete(_logger, LastRunSucceeded, LastRunDuration, null);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The first run is triggered by the host, subsequent runs are delayed
        try
        {
            await RunOnceAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // This is the normal shutdown signal, no need to log an error
            _serviceStopping(_logger, null);
        }
        catch (DbUpdateException ex)
        {
            _initialRunDbFailed(_logger, ex);
        }
        catch (HttpRequestException ex)
        {
            _initialRunHttpFailed(_logger, ex);
        }
        catch (JsonException ex)
        {
            _initialRunJsonFailed(_logger, ex);
        }

        var intervalSec = int.TryParse(config["KEYCLOAK_SYNC_INTERVAL_SEC"], out var s) ? s : 300;
        var delay = TimeSpan.FromSeconds(Math.Max(30, intervalSec));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // This is the normal shutdown signal, no need to log an error
                _serviceStopping(_logger, null);
            }
            catch (DbUpdateException ex)
            {
                _initialRunDbFailed(_logger, ex);
            }
            catch (HttpRequestException ex)
            {
                _initialRunHttpFailed(_logger, ex);
            }
            catch (JsonException ex)
            {
                _initialRunJsonFailed(_logger, ex);
            }
        }
    }

    private static string? ResolveRealmFromIssuer(string issuer)
    {
        if (string.IsNullOrWhiteSpace(issuer))
        {
            return null;
        }

        var idx = issuer.IndexOf("/realms/", StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? issuer[(idx + 8)..] : null; // 8 = length of "/realms/"
    }

    private static string? ResolveAdminBase(string issuer, string realm)
    {
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(realm))
        {
            return null;
        }

        var suffix = $"/realms/{realm}";
        if (issuer.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return issuer[..^suffix.Length];
        }

        try
        {
            return new Uri(issuer).GetLeftPart(UriPartial.Authority);
        }
        catch (UriFormatException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool HasScheme(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        if (s.Contains("://", StringComparison.Ordinal))
        {
            return true;
        }

        return Uri.TryCreate(s, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Scheme);
    }

    internal List<KeycloakUserDto> ParseKeycloakUsers(string text)
    {
        var list = new List<KeycloakUserDto>();
        try
        {
            var arr = JsonSerializer.Deserialize<List<JsonElement>>(text, _json) ?? [];
            foreach (var el in arr)
            {
                var id = el.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty;
                var username = el.TryGetProperty("username", out var uEl)
                    ? uEl.GetString() ?? string.Empty
                    : string.Empty;
                var email = el.TryGetProperty("email", out var eEl) ? eEl.GetString() ?? string.Empty : string.Empty;
                var first = el.TryGetProperty("firstName", out var fEl)
                    ? fEl.GetString() ?? string.Empty
                    : string.Empty;
                var last = el.TryGetProperty("lastName", out var lEl) ? lEl.GetString() ?? string.Empty : string.Empty;
                if (!string.IsNullOrWhiteSpace(email))
                {
                    list.Add(new KeycloakUserDto(id, username, email, first, last));
                }
            }
        }
        catch (JsonException ex)
        {
            LogMessages.Warning_Msg(_logger, ex);
        }
        catch (Exception ex)
        {
            LogMessages.Warning_Msg(_logger, ex);
        }

        return list;
    }

    private async Task<(bool IsSeller, bool IsAdmin)> GetRolesForUserAsync(KeycloakAdminContext ctx,
        string id, CancellationToken ct)
    {
        var rolesJson = await FetchRolesJsonAsync(ctx, id, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(rolesJson))
        {
            return (false, false);
        }

        return ParseRolesFromJson(rolesJson);
    }

    internal (bool IsSeller, bool IsAdmin) ParseRolesFromJson(string rolesJson)
    {
        if (string.IsNullOrWhiteSpace(rolesJson))
        {
            return (false, false);
        }

        try
        {
            var roles = JsonSerializer.Deserialize<List<JsonElement>>(rolesJson, _json);
            if (roles == null)
            {
                return (false, false);
            }

            var isSeller = false;
            var isAdmin = false;
            foreach (var role in roles)
            {
                if (!role.TryGetProperty("name", out var roleName))
                {
                    continue;
                }

                var name = roleName.GetString();
                if (string.Equals(name, "seller", StringComparison.OrdinalIgnoreCase))
                {
                    isSeller = true;
                }

                if (string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    isAdmin = true;
                }

                if (isSeller && isAdmin)
                {
                    break;
                }
            }

            return (isSeller, isAdmin);
        }
        catch (JsonException ex)
        {
            LogMessages.Warning_Msg(_logger, ex);
            return (false, false);
        }
        catch (Exception ex)
        {
            LogMessages.Warning_Msg(_logger, ex);
            return (false, false);
        }
    }

    private async Task<string?> FetchRolesJsonAsync(KeycloakAdminContext ctx, string id, CancellationToken ct)
    {
        try
        {
            var rolesUrl =
                $"{ctx.AdminBase}/admin/realms/{ctx.Realm}/users/{Uri.EscapeDataString(id)}/role-mappings/realm";
            using var rolesResp = await ctx.Http.GetAsync(new Uri(rolesUrl), ct).ConfigureAwait(false);
            if (!rolesResp.IsSuccessStatusCode)
            {
                return null;
            }

            return await rolesResp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            LogMessages.Warning_Id(_logger, id, ex);
            return null;
        }
        catch (Exception ex)
        {
            LogMessages.Warning_Id(_logger, id, ex);
            return null;
        }
    }

    private async Task UpsertProfilesAsync(IEnumerable<KeycloakUserDto> users, KeycloakAdminContext ctx,
        CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();

        foreach (var u in users)
        {
            await UpsertSingleUserAsync(db, ctx, u, ct).ConfigureAwait(false);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false); // Save changes after all upserts
    }

    private async Task UpsertSingleUserAsync(UserDbContext db, KeycloakAdminContext ctx, KeycloakUserDto u,
        CancellationToken ct)
    {
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Email == u.Email, ct).ConfigureAwait(false);
        var userIdGuid = Guid.TryParse(u.Id, out var parsed) ? parsed : Guid.NewGuid();

        var (isSeller, isAdmin) = await GetRolesForUserAsync(ctx, u.Id, ct).ConfigureAwait(false);

        if (profile == null)
        {
            await CreateProfileAsync(db, u, userIdGuid, isSeller, isAdmin, ct).ConfigureAwait(false);
            return;
        }

        await UpdateProfileIfNeededAsync(db, profile, u, isSeller, isAdmin, ct).ConfigureAwait(false);
    }

    private async Task CreateProfileAsync(UserDbContext db, KeycloakUserDto u, Guid userIdGuid, bool isSeller,
        bool isAdmin, CancellationToken ct)
    {
        var phoneNumber = GenerateSamplePhoneNumber(u.Username);
        var (address, city, postalCode, country) = GenerateSampleAddress(u.Username);

        var display = !string.IsNullOrWhiteSpace(u.First) || !string.IsNullOrWhiteSpace(u.Last)
            ? $"{u.First} {u.Last}".Trim()
            : u.Username;
        var full = $"{u.First} {u.Last}".Trim();
        if (string.IsNullOrWhiteSpace(display))
        {
            display = u.Username;
        }

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userIdGuid,
            KeycloakId = u.Id,
            Email = u.Email,
            DisplayName = display,
            FullName = full,
            PhoneNumber = phoneNumber,
            Address = address,
            City = city,
            PostalCode = postalCode,
            Country = country,
            IsAdmin = isAdmin,
            IsSeller = isSeller,
            IsSellerVerified = isSeller,
            CreatedAt = clock.UtcNow
        };
        db.Profiles.Add(profile);
        // SaveChanges is called in UpsertProfilesAsync after all upserts
        _logger.LogInformation(
            "[KeycloakSync] Created profile for {Username} (DisplayName: {DisplayName}, Seller: {IsSeller}, Admin: {IsAdmin})",
            u.Username, profile.DisplayName, isSeller, isAdmin);
    }

    private async Task UpdateProfileIfNeededAsync(UserDbContext db, UserProfile profile, KeycloakUserDto u,
        bool isSeller, bool isAdmin, CancellationToken ct)
    {
        var changed = ApplyProfileUpdates(profile, u, isSeller, isAdmin);
        if (!changed)
        {
            return;
        }

        profile.UpdatedAt = clock.UtcNow;
        db.Profiles.Update(profile);
        // SaveChanges is called in UpsertProfilesAsync after all users are processed
        _logger.LogInformation(
            "[KeycloakSync] Updated profile for {Username} (DisplayName: {DisplayName}, Seller: {IsSeller}, Admin: {IsAdmin})",
            u.Username, profile.DisplayName, isSeller, isAdmin);
    }

    private static bool ApplyProfileUpdates(UserProfile profile, KeycloakUserDto u, bool isSeller, bool isAdmin)
    {
        var changed = false;
        var newDisplay = !string.IsNullOrWhiteSpace(u.First) || !string.IsNullOrWhiteSpace(u.Last)
            ? $"{u.First} {u.Last}".Trim()
            : u.Username;
        var newFull = $"{u.First} {u.Last}".Trim();
        if (string.IsNullOrWhiteSpace(newDisplay))
        {
            newDisplay = u.Username;
        }

        if (profile.DisplayName != newDisplay)
        {
            profile.DisplayName = newDisplay;
            changed = true;
        }

        if (profile.FullName != newFull)
        {
            profile.FullName = newFull;
            changed = true;
        }

        if (profile.Email != u.Email)
        {
            profile.Email = u.Email;
            changed = true;
        }

        if (profile.KeycloakId != u.Id)
        {
            profile.KeycloakId = u.Id;
            changed = true;
        }

        if (profile.IsSeller != isSeller)
        {
            profile.IsSeller = isSeller;
            changed = true;
        }

        if (profile.IsAdmin != isAdmin)
        {
            profile.IsAdmin = isAdmin;
            changed = true;
        }

        if (isSeller && !profile.IsSellerVerified)
        {
            profile.IsSellerVerified = true;
            changed = true;
        }

        return changed;
    }

    // Helper methods to generate sample contact and address data for demo users
    internal static string GenerateSamplePhoneNumber(string username)
    {
        // Generate consistent phone numbers based on username
        var hash = Math.Abs(StringComparer.Ordinal.GetHashCode(username ?? string.Empty));
        var phoneBase = 40000000 + hash % 59999999; // Norwegian mobile format starts with 4 or 9
        return $"+47 {phoneBase:### ## ###}";
    }

    internal static (string address, string city, string postalCode, string country) GenerateSampleAddress(
        string username)
    {
        // Sample Norwegian cities and addresses for demo data
        var cities = new[]
        {
            ("Oslo", "0150", ["Karl Johans gate", "Grensen", "Storgata", "Akersveien"]), ("Bergen", "5003", ["Bryggen", "Øvregaten", "Marken", "Torgallmenningen"]), ("Trondheim", "7011", ["Munkegata", "Nordre gate", "Kongens gate", "Kjøpmannsgata"]),
            ("Stavanger", "4006", ["Øvre Holmegate", "Kirkegata", "Skagen", "Nytorget"]), ("Kristiansand", "4611", new[]
            {
                "Markens gate", "Vestre Strandgate", "Elvegata", "Dronningens gate"
            })
        };

        // Use username hash to consistently select city and street
        var hash = Math.Abs(StringComparer.Ordinal.GetHashCode(username ?? string.Empty));
        var cityData = cities[hash % cities.Length];
        var street = cityData.Item3[hash % cityData.Item3.Length];
        var streetNumber = 1 + hash % 199; // Street numbers 1-200

        return (
            address: $"{street} {streetNumber}",
            city: cityData.Item1,
            postalCode: cityData.Item2,
            country: "Norway"
            );
    }

    // Small retry utility for async ops that might fail transiently during cold start
    private async Task<T?> RetryAsync<T>(Func<Task<T>> op, CancellationToken ct, int attempts = 3,
        int delayMs = 500)
    {
        Exception? last = null;
        for (var i = 0; i < attempts && !ct.IsCancellationRequested; i++)
        {
            try
            {
                return await op().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw; // Propagate cancellation
            }
            catch (HttpRequestException ex)
            {
                last = ex;
                LogMessages.Warning_Attempt_Attempts(_logger, i + 1,
                    attempts, ex);
                await Task.Delay(delayMs * (i + 1), ct).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                last = ex;
                LogMessages.Warning_Attempt_Attempts(_logger, i + 1,
                    attempts, ex);
                await Task.Delay(delayMs * (i + 1), ct).ConfigureAwait(false);
            }
            catch (DbUpdateException ex)
            {
                last = ex;
                LogMessages.Warning_Attempt_Attempts(_logger, i + 1,
                    attempts, ex);
                await Task.Delay(delayMs * (i + 1), ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                last = ex;
                LogMessages.Warning_Attempt_Attempts(_logger, i + 1, attempts, ex);
                await Task.Delay(delayMs * (i + 1), ct).ConfigureAwait(false);
            }
        }

        if (last != null)
        {
            throw last;
        }

        return default;
    }

    internal record KeycloakAdminContext(HttpClient Http, string AdminBase, string Realm);

    internal record KeycloakUserDto(string Id, string Username, string Email, string First, string Last);
}
