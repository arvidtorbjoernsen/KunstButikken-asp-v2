using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using KunstButikken.UserService.Data;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.UserService.Services;

public class KeycloakSeeder(
    ILogger<KeycloakSeeder> logger,
    IHttpClientFactory httpFactory,
    IConfiguration config,
    IServiceProvider services)
    : IKeycloakSeeder
{
    // Define LoggerMessage delegates to avoid allocations (CA1848)
    private static readonly Action<ILogger, Exception?> _applyingMigrations = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(1000, nameof(ApplyMigrationsAsync)),
        "Applying database migrations...");

    private static readonly Action<ILogger, Exception?> _migrationsApplied = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(1001, "MigrationsApplied"),
        "Database migrations applied successfully.");

    private static readonly Action<ILogger, int, double, Exception?> _migrationAttemptFailed = LoggerMessage.Define<int, double>(
        LogLevel.Warning,
        new EventId(1002, "MigrationAttemptFailed"),
        "Database migration attempt {Attempt} failed, retrying in {Delay} seconds...");

    private static readonly Action<ILogger, int, Exception?> _migrationsFailedFinal = LoggerMessage.Define<int>(
        LogLevel.Error,
        new EventId(1003, "MigrationsFailedFinal"),
        "Database migration failed after {MaxRetries} attempts. The service might not function correctly.");

    private static readonly Action<ILogger, Exception?> _failedToObtainAdminToken = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(1100, "FailedToObtainAdminToken"),
        "Failed to obtain Keycloak admin token. Skipping seeding.");

    private static readonly Action<ILogger, string, Exception?> _realmEmpty = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(1101, "RealmEmpty"),
        "Keycloak realm '{Realm}' is empty. Seeding initial users...");

    private static readonly Action<ILogger, int, Exception?> _seedingCompleted = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(1102, "SeedingCompleted"),
        "Seeding completed. Created {Count} users in Keycloak.");

    private static readonly Action<ILogger, Exception?> _skippingMissingConfig = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(1103, "SkippingMissingConfig"),
        "Skipping Keycloak seeding: Missing KEYCLOAK_ISSUER or KEYCLOAK_REALM configuration.");

    private static readonly Action<ILogger, string, Exception?> _couldNotResolveAdminBase = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(1104, "CouldNotResolveAdminBase"),
        "Could not resolve Keycloak admin base from issuer {Issuer}. Skipping seeding.");

    private static readonly Action<ILogger, string, Exception?> _realmContainsUsers = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(1105, "RealmContainsUsers"),
        "Keycloak realm '{Realm}' already contains users. Skipping seeding.");

    private static readonly Action<ILogger, Exception?> _errorCheckingUsers = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(1106, "ErrorCheckingUsers"),
        "Error checking existing Keycloak users. Skipping seeding.");

    private static readonly Action<ILogger, string, HttpStatusCode, string, string, Exception?> _failedToCreateUser = LoggerMessage.Define<string, HttpStatusCode, string, string>(
        LogLevel.Error,
        new EventId(1200, "FailedToCreateUser"),
        "Failed to create user {Username}: {StatusCode} {ReasonPhrase} - {ErrorContent}");

    private static readonly Action<ILogger, string, Exception?> _userCreated = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(1201, "UserCreated"),
        "Successfully created user: {Username}");

    private static readonly Action<ILogger, string, Exception?> _couldNotGetUserId = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(1202, "CouldNotGetUserId"),
        "Could not get user ID for {Username}, skipping role assignment");

    private static readonly Action<ILogger, string, Exception?> _userHasNoRoles = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(1203, "UserHasNoRoles"),
        "User {Username} has no roles to assign");

    private static readonly Action<ILogger, string, string, Exception?> _assigningRoles = LoggerMessage.Define<string, string>(
        LogLevel.Information,
        new EventId(1204, "AssigningRoles"),
        "Assigning roles to user {Username} (ID: {UserId})");

    private static readonly Action<ILogger, string, Exception?> _errorCreatingUser = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(1205, "ErrorCreatingUser"),
        "Error creating user {Username}");

    private static readonly Action<ILogger, string, string, Exception?> _roleExists = LoggerMessage.Define<string, string>(
        LogLevel.Information,
        new EventId(1300, "RoleExists"),
        "Role '{RoleName}' already exists in realm '{Realm}'");

    private static readonly Action<ILogger, string, string, Exception?> _roleCreated = LoggerMessage.Define<string, string>(
        LogLevel.Information,
        new EventId(1301, "RoleCreated"),
        "Successfully created role '{RoleName}' in realm '{Realm}'");

    private static readonly Action<ILogger, string, HttpStatusCode, string, Exception?> _failedToCreateRole = LoggerMessage.Define<string, HttpStatusCode, string>(
        LogLevel.Error,
        new EventId(1302, "FailedToCreateRole"),
        "Failed to create role '{RoleName}': {StatusCode} - {ErrorContent}");

    private static readonly Action<ILogger, string, Exception?> _errorEnsuringRole = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(1303, "ErrorEnsuringRole"),
        "Error ensuring role '{RoleName}' exists");

    private static readonly Action<ILogger, string, Exception?> _searchingForUser = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(1400, "SearchingForUser"),
        "Searching for user: {SearchUrl}");

    private static readonly Action<ILogger, string, HttpStatusCode, Exception?> _failedToGetUserIdStatus = LoggerMessage.Define<string, HttpStatusCode>(
        LogLevel.Warning,
        new EventId(1401, "FailedToGetUserIdStatus"),
        "Failed to get user ID for {Username}: {StatusCode}");

    private static readonly Action<ILogger, string, Exception?> _userSearchResponse = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(1402, "UserSearchResponse"),
        "User search response: {Json}");

    private static readonly Action<ILogger, string, string, Exception?> _foundUserId = LoggerMessage.Define<string, string>(
        LogLevel.Information,
        new EventId(1403, "FoundUserId"),
        "Found user ID for {Username}: {UserId}");

    private static readonly Action<ILogger, string, Exception?> _userNotFoundAfterCreation = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(1404, "UserNotFoundAfterCreation"),
        "User {Username} not found after creation");

    private static readonly Action<ILogger, string, string, Exception?> _roleNotFoundForAssignment = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(1500, "RoleNotFoundForAssignment"),
        "Role '{RoleName}' not found in realm '{Realm}'. Skipping role assignment.");

    private static readonly Action<ILogger, string, Exception?> _noValidRolesToAssign = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(1501, "NoValidRolesToAssign"),
        "No valid roles found to assign to user {UserId}");

    private static readonly Action<ILogger, string, string, Exception?> _assignedRoles = LoggerMessage.Define<string, string>(
        LogLevel.Information,
        new EventId(1502, "AssignedRoles"),
        "Successfully assigned roles {Roles} to user {UserId}");

    private static readonly Action<ILogger, string, HttpStatusCode, string, Exception?> _failedToAssignRoles = LoggerMessage.Define<string, HttpStatusCode, string>(
        LogLevel.Error,
        new EventId(1503, "FailedToAssignRoles"),
        "Failed to assign roles to user {UserId}: {StatusCode} - {ErrorContent}");

    private static readonly Action<ILogger, string, Exception?> _errorAssigningRoles = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(1504, "ErrorAssigningRoles"),
        "Error assigning roles to user {UserId}");

    private static readonly Action<ILogger, string, Exception?> _attemptingClientCreds = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(1600, "AttemptingClientCreds"),
        "Attempting to get admin token via client_credentials grant for client '{ClientId}'...");

    private static readonly Action<ILogger, Exception?> _noAdminClientId = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(1601, "NoAdminClientId"),
        "No admin client ID specified, defaulting to 'admin-cli' for password grant.");

    private static readonly Action<ILogger, string, string, Exception?> _attemptingPasswordGrant = LoggerMessage.Define<string, string>(
        LogLevel.Information,
        new EventId(1602, "AttemptingPasswordGrant"),
        "Attempting to get admin token via password grant for user '{Username}' with client '{ClientId}'...");

    private static readonly Action<ILogger, Exception?> _failedToObtainAdminTokenWarning = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(1603, "FailedToObtainAdminTokenWarning"),
        "Failed to obtain admin token. Check KEYCLOAK_ADMIN_CLIENT_ID/SECRET or KEYCLOAK_ADMIN_USERNAME/PASSWORD configuration.");

    private static readonly Action<ILogger, HttpStatusCode, string, Exception?> _clientCredsFailed = LoggerMessage.Define<HttpStatusCode, string>(
        LogLevel.Warning,
        new EventId(1604, "ClientCredsFailed"),
        "Client credentials token request failed: {StatusCode} {ReasonPhrase}");

    private static readonly Action<ILogger, HttpStatusCode, string, Exception?> _passwordGrantFailed = LoggerMessage.Define<HttpStatusCode, string>(
        LogLevel.Warning,
        new EventId(1605, "PasswordGrantFailed"),
        "Password grant token request failed: {StatusCode} {ReasonPhrase}");

    private static readonly Action<ILogger, Exception?> _tokenRequestFailedAfterRetries = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(1606, "TokenRequestFailedAfterRetries"),
        "Keycloak admin token request failed after retries");

    private readonly JsonSerializerOptions _camelCaseJsonOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        _applyingMigrations(logger, null);

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();

        const int maxRetries = 10;
        var retryDelay = TimeSpan.FromSeconds(5);

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
                _migrationsApplied(logger, null);
                return; // Success
            }
            catch (Exception ex) when (attempt < maxRetries - 1)
            {
                _migrationAttemptFailed(logger, attempt + 1, retryDelay.TotalSeconds, ex);
                await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _migrationsFailedFinal(logger, maxRetries, ex);
            }
        }
    }

    public async Task SeedKeycloakUsersAsync(CancellationToken ct = default)
    {
        var options = ResolveKeycloakOptions();
        if (options == null)
        {
            return;
        }

        using var http = httpFactory.CreateClient();
        var adminToken = await TryObtainAdminTokenAsync(http, options, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(adminToken))
        {
            _failedToObtainAdminToken(logger, null);
            return;
        }

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var keycloakContext = new KeycloakContext(http, options.AdminBase, options.Realm);

        if (!await IsSeedingNeededAsync(keycloakContext, ct).ConfigureAwait(false))
        {
            return;
        }

        _realmEmpty(logger, options.Realm, null);

        await EnsureRealmRolesExistAsync(keycloakContext, ct).ConfigureAwait(false);

        var usersToSeed = GetInitialUsers();
        var usersCreated = 0;

        foreach (var user in usersToSeed)
        {
            if (await CreateUserWithRolesAsync(keycloakContext, user, ct).ConfigureAwait(false))
            {
                usersCreated++;
            }
        }

        _seedingCompleted(logger, usersCreated, null);
    }

    // The rest of the helper methods mirror the logic previously in KeycloakSeedingHostedService.
    // To keep this edit concise, include the same helper implementations here (copied and adapted to use _logger/_config/_httpFactory/_services).

    private KeycloakAdminOptions? ResolveKeycloakOptions()
    {
        var issuer = GetConfigValue("KEYCLOAK_ISSUER", "NEXT_PUBLIC_KEYCLOAK_ISSUER");
        var realm = GetConfigValue("KEYCLOAK_REALM") ?? ResolveRealmFromIssuer(issuer);

        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(realm))
        {
            _skippingMissingConfig(logger, null);
            return null;
        }

        var adminBase = ResolveAdminBase(issuer, realm);
        if (IsAspireEndpointReference(issuer))
        {
            var discoverableBase = config["KEYCLOAK_BASE"];
            adminBase = !string.IsNullOrWhiteSpace(discoverableBase)
                ? discoverableBase!.TrimEnd('/')
                : "http://keycloak";
        }

        if (string.IsNullOrWhiteSpace(adminBase))
        {
            _couldNotResolveAdminBase(logger, issuer, null);
            return null;
        }

        if (!HasScheme(adminBase))
        {
            adminBase = "http://" + adminBase.TrimStart('/');
        }

        return new KeycloakAdminOptions(
            issuer,
            realm,
            adminBase,
            GetConfigValue("KEYCLOAK_ADMIN_CLIENT_ID", "KEYCLOAK_CLIENT_ID_ADMIN"),
            GetConfigValue("KEYCLOAK_ADMIN_CLIENT_SECRET", "KEYCLOAK_CLIENT_SECRET_ADMIN"),
            GetConfigValue("KC_BOOTSTRAP_ADMIN_USERNAME", "KEYCLOAK_ADMIN_USERNAME", "KEYCLOAK_ADMIN_USER"),
            GetConfigValue("KC_BOOTSTRAP_ADMIN_PASSWORD", "KEYCLOAK_ADMIN_PASSWORD"),
            GetConfigValue("KEYCLOAK_ADMIN_TOKEN_REALM")
        );
    }

    private string? GetConfigValue(params string[] keys) =>
        keys.Select(key => config[key]).FirstOrDefault(value => !string.IsNullOrEmpty(value));

    private static bool IsAspireEndpointReference(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains("aspire.hosting.applicationmodel.endpointreference", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> IsSeedingNeededAsync(KeycloakContext ctx, CancellationToken ct)
    {
        var usersUrl = $"{ctx.AdminBase}/admin/realms/{ctx.Realm}/users?max=1"; // Just fetch one to check if any exist
        try
        {
            using var res = await ctx.Http.GetAsync(new Uri(usersUrl), ct).ConfigureAwait(false);
            res.EnsureSuccessStatusCode();
            var text = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var arr = JsonSerializer.Deserialize<List<JsonElement>>(text, _jsonOptions) ?? [];
            if (arr.Count > 0)
            {
                _realmContainsUsers(logger, ctx.Realm, null);
                return false;
            }
        }
        catch (Exception ex)
        {
            _errorCheckingUsers(logger, ex);
            return false;
        }

        return true;
    }

    private async Task<bool> CreateUserWithRolesAsync(KeycloakContext ctx, KeycloakUser user, CancellationToken ct)
    {
        try
        {
            var userCreationUrl = $"{ctx.AdminBase}/admin/realms/{ctx.Realm}/users";
            var userJson = JsonSerializer.Serialize(user, _camelCaseJsonOptions);
            using var content = new StringContent(userJson, Encoding.UTF8, "application/json");

            using var response = await ctx.Http.PostAsync(new Uri(userCreationUrl), content, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _failedToCreateUser(logger, user.Username, response.StatusCode, response.ReasonPhrase ?? string.Empty, errorContent, null);
                return false;
            }

            _userCreated(logger, user.Username, null);

            var userId = await GetUserIdAsync(ctx, user.Username, ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _couldNotGetUserId(logger, user.Username, null);
                return true;
            }

            if (user.RealmRoles.Count == 0)
            {
                _userHasNoRoles(logger, user.Username, null);
                return true;
            }

            _assigningRoles(logger, user.Username, userId, null);
            await AssignRealmRolesToUserAsync(ctx, userId, user.RealmRoles, ct).ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            _errorCreatingUser(logger, user?.Username ?? string.Empty, ex);
            return false;
        }
    }

    private async Task EnsureRealmRolesExistAsync(KeycloakContext ctx, CancellationToken ct)
    {
        var requiredRoles = new[]
        {
            "admin", "seller", "buyer"
        };

        foreach (var roleName in requiredRoles)
        {
            try
            {
                var roleUrl = $"{ctx.AdminBase}/admin/realms/{ctx.Realm}/roles/{Uri.EscapeDataString(roleName)}";
                using var checkResponse = await ctx.Http.GetAsync(new Uri(roleUrl), ct).ConfigureAwait(false);

                if (checkResponse.IsSuccessStatusCode)
                {
                    _roleExists(logger, roleName!, ctx.Realm!, null);
                    continue;
                }

                var createRoleUrl = $"{ctx.AdminBase}/admin/realms/{ctx.Realm}/roles";
                var rolePayload = new
                {
                    name = roleName, description = $"{roleName} role"
                };
                var roleJson = JsonSerializer.Serialize(rolePayload, _camelCaseJsonOptions);
                using var content = new StringContent(roleJson, Encoding.UTF8, "application/json");

                using var createResponse =
                    await ctx.Http.PostAsync(new Uri(createRoleUrl), content, ct).ConfigureAwait(false);

                if (createResponse.IsSuccessStatusCode)
                {
                    _roleCreated(logger, roleName!, ctx.Realm!, null);
                }
                else
                {
                    var errorContent = await createResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _failedToCreateRole(logger, roleName!, createResponse.StatusCode, errorContent ?? string.Empty, null);
                }
            }
            catch (Exception ex)
            {
                _errorEnsuringRole(logger, roleName, ex);
            }
        }
    }

    private async Task<string?> GetUserIdAsync(KeycloakContext ctx, string username, CancellationToken ct)
    {
        try
        {
            var searchUrl =
                $"{ctx.AdminBase}/admin/realms/{ctx.Realm}/users?username={Uri.EscapeDataString(username)}&exact=true";
            _searchingForUser(logger, searchUrl, null);

            using var response = await ctx.Http.GetAsync(new Uri(searchUrl), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _failedToGetUserIdStatus(logger, username, response.StatusCode, null);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _userSearchResponse(logger, json, null);

            var users = JsonSerializer.Deserialize<List<JsonElement>>(json, _jsonOptions);

            if (users is { Count: > 0 } && users[0].TryGetProperty("id", out var idElement))
            {
                var userId = idElement.GetString();
                _foundUserId(logger, username, userId ?? string.Empty, null);
                return userId;
            }

            _userNotFoundAfterCreation(logger, username ?? string.Empty, null);
            return null;
        }
        catch (Exception ex)
        {
            _errorEnsuringRole(logger, username ?? string.Empty, ex);
            return null;
        }
    }

    private async Task AssignRealmRolesToUserAsync(KeycloakContext ctx, string userId, List<string> roleNames,
        CancellationToken ct)
    {
        try
        {
            var roleRepresentations = new List<object>();

            foreach (var roleName in roleNames)
            {
                var roleUrl = $"{ctx.AdminBase}/admin/realms/{ctx.Realm}/roles/{Uri.EscapeDataString(roleName)}";
                using var roleResponse = await ctx.Http.GetAsync(new Uri(roleUrl), ct).ConfigureAwait(false);

                if (!roleResponse.IsSuccessStatusCode)
                {
                    _roleNotFoundForAssignment(logger, roleName, ctx.Realm, null);
                    continue;
                }

                var roleJson = await roleResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var roleElement = JsonSerializer.Deserialize<JsonElement>(roleJson, _jsonOptions);

                if (roleElement.TryGetProperty("id", out var roleId) &&
                    roleElement.TryGetProperty("name", out var name))
                {
                    roleRepresentations.Add(new
                    {
                        id = roleId.GetString(), name = name.GetString()
                    });
                }
            }

            if (roleRepresentations.Count == 0)
            {
                _noValidRolesToAssign(logger, userId, null);
                return;
            }

            var assignRoleUrl = $"{ctx.AdminBase}/admin/realms/{ctx.Realm}/users/{userId}/role-mappings/realm";
            var assignRoleJson = JsonSerializer.Serialize(roleRepresentations, _camelCaseJsonOptions);
            using var assignContent = new StringContent(assignRoleJson, Encoding.UTF8, "application/json");

            using var assignResponse =
                await ctx.Http.PostAsync(new Uri(assignRoleUrl), assignContent, ct).ConfigureAwait(false);

            if (assignResponse.IsSuccessStatusCode)
            {
                _assignedRoles(logger, string.Join(", ", roleNames), userId, null);
            }
            else
            {
                var errorContent = await assignResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _failedToAssignRoles(logger, userId, assignResponse.StatusCode, errorContent, null);
            }
        }
        catch (Exception ex)
        {
            _errorAssigningRoles(logger, userId, ex);
        }
    }

    private static string? ResolveRealmFromIssuer(string? issuer)
    {
        if (string.IsNullOrWhiteSpace(issuer))
        {
            return null;
        }

        var idx = issuer.IndexOf("/realms/", StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? issuer.Substring(idx + 8) : null; // 8 = length of "/realms/"
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
            return issuer.Substring(0, issuer.Length - suffix.Length);
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

    private async Task<string?> TryObtainAdminTokenAsync(HttpClient http, KeycloakAdminOptions options,
        CancellationToken ct)
    {
        var tokenRealmToUse = string.IsNullOrWhiteSpace(options.OverrideTokenRealm)
            ? "master"
            : options.OverrideTokenRealm;
        var tokenEndpoint =
            new Uri($"{options.AdminBase.TrimEnd('/')}/realms/{tokenRealmToUse}/protocol/openid-connect/token");
        // Diagnostic (safe): log which token endpoint we're going to call and which strategies are available
        try
        {
            logger.LogInformation("[KeycloakSeeder] Token endpoint resolved to: {TokenEndpoint}", tokenEndpoint);
            logger.LogInformation("[KeycloakSeeder] Token realm: {TokenRealm}; ClientCredsAvailable: {HasClientCreds}; PasswordCredsAvailable: {HasPasswordCreds}",
                tokenRealmToUse,
                !string.IsNullOrWhiteSpace(options.AdminClientId) && !string.IsNullOrWhiteSpace(options.AdminClientSecret),
                !string.IsNullOrWhiteSpace(options.AdminUsername) && !string.IsNullOrWhiteSpace(options.AdminPassword)
            );
        }
        catch
        {
            // Logging must never throw — swallow any logging exceptions
        }
        var tokenContext = new TokenRequestContext(http, tokenEndpoint, options);

        var strategies = new List<Func<Task<string?>>>();

        if (tokenContext.HasClientCredentials)
        {
            strategies.Add(() =>
            {
                _attemptingClientCreds(logger, tokenContext.ClientId, null);
                return RetryAsync(() => GetTokenClientCredsAsync(tokenContext, ct));
            });
        }

        if (tokenContext.HasPasswordCredentials)
        {
            var clientId = tokenContext.ClientId;
            if (string.IsNullOrWhiteSpace(clientId))
            {
                clientId = "admin-cli";
                _noAdminClientId(logger, null);
            }

            strategies.Add(() =>
            {
                _attemptingPasswordGrant(logger, options.AdminUsername ?? string.Empty, clientId, null);
                return RetryAsync(() => GetTokenPasswordAsync(tokenContext with
                {
                    ClientId = clientId
                }, ct));
            });
        }

        foreach (var strategy in strategies)
        {
            var token = await strategy().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(token))
            {
                return token;
            }
        }

        _failedToObtainAdminTokenWarning(logger, null);
        return null;
    }

    private async Task<string?> GetTokenClientCredsAsync(TokenRequestContext ctx, CancellationToken ct)
    {
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials", ["client_id"] = ctx.ClientId, ["client_secret"] = ctx.Options.AdminClientSecret!
        };
        // Dispose FormUrlEncodedContent to avoid CA2000 resource leak
        using var content = new FormUrlEncodedContent(body);
        using var res = await ctx.Http.PostAsync(ctx.TokenEndpoint, content, ct).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
        {
            _clientCredsFailed(logger, res.StatusCode, res.ReasonPhrase ?? string.Empty, null);
            return null;
        }

        // Await the stream, parse, then DisposeAsync() with ConfigureAwait(false) in finally so
        // JsonDocument.ParseAsync receives a real Stream and all ConfigureAwait(false) usages remain.
        var st = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        try
        {
            var json = await JsonDocument.ParseAsync(st, cancellationToken: ct).ConfigureAwait(false);
            return json.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        }
        finally
        {
            await st.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task<string?> GetTokenPasswordAsync(TokenRequestContext ctx, CancellationToken ct)
    {
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "password", ["client_id"] = ctx.ClientId, ["username"] = ctx.Options.AdminUsername!, ["password"] = ctx.Options.AdminPassword!
        };
        if (!string.IsNullOrEmpty(ctx.Options.AdminClientSecret))
        {
            body["client_secret"] = ctx.Options.AdminClientSecret;
        }

        using var content = new FormUrlEncodedContent(body);
        using var res = await ctx.Http.PostAsync(ctx.TokenEndpoint, content, ct).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
        {
            _passwordGrantFailed(logger, res.StatusCode, res.ReasonPhrase ?? string.Empty, null);
            return null;
        }

        // Same fix as above: await stream, parse, and dispose with ConfigureAwait(false).
        var st2 = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        try
        {
            var json = await JsonDocument.ParseAsync(st2, cancellationToken: ct).ConfigureAwait(false);
            return json.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        }
        finally
        {
            await st2.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task<string?> RetryAsync(Func<Task<string?>> fn)
    {
        Exception? lastEx = null;
        for (var i = 0; i < 3; i++)
        {
            try
            {
                var t = await fn().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(t))
                {
                    return t;
                }
            }
            catch (Exception ex)
            {
                lastEx = ex;
            }

            await Task.Delay(200 * (i + 1)).ConfigureAwait(false);
        }

        if (lastEx != null)
        {
            _tokenRequestFailedAfterRetries(logger, lastEx);
        }

        return null;
    }

    private List<KeycloakUser> GetInitialUsers()
    {
        var userDefinitions = GetAdminAndSellerUsers();
        userDefinitions.AddRange(GetBuyerUsers());
        return userDefinitions.Select(CreateKeycloakUser).ToList();
    }

    private static List<UserDefinition> GetAdminAndSellerUsers()
    {
        return
        [
            new UserDefinition("admin", "admin@example.com", "System", "Admin", "admin", "admin"),
            new UserDefinition("seller1", "seller1@example.com", "Selma", "Selger", "seller", "seller"),
            new UserDefinition("seller2", "seller2@example.com", "Sindre", "Selger", "seller", "seller"),
            new UserDefinition("seller3", "seller3@example.com", "Sigrid", "Selger", "seller", "seller")
        ];
    }

    private static List<UserDefinition> GetBuyerUsers()
    {
        var users = new List<UserDefinition>();
        for (var i = 1; i <= 7; i++)
        {
            users.Add(new UserDefinition($"buyer{i}", $"buyer{i}@example.com", "Buyer", $"{i}", "buyer", "buyer"));
        }

        return users;
    }

    private KeycloakUser CreateKeycloakUser(UserDefinition userDef)
    {
        return new KeycloakUser(
            userDef.Username,
            userDef.Email,
            userDef.FirstName,
            userDef.LastName,
            true,
            [new KeycloakCredential("password", userDef.Password, false)],
            [.. userDef.Roles]
        );
    }

    private record KeycloakContext(HttpClient Http, string AdminBase, string Realm);

    private record KeycloakAdminOptions(
        string Issuer,
        string Realm,
        string AdminBase,
        string? AdminClientId,
        string? AdminClientSecret,
        string? AdminUsername,
        string? AdminPassword,
        string? OverrideTokenRealm
    );

    private record TokenRequestContext(HttpClient Http, Uri TokenEndpoint, KeycloakAdminOptions Options)
    {
        public string ClientId { get; init; } = Options.AdminClientId ?? string.Empty;

        public bool HasClientCredentials =>
            !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(Options.AdminClientSecret);

        public bool HasPasswordCredentials => !string.IsNullOrWhiteSpace(Options.AdminUsername) &&
                                              !string.IsNullOrWhiteSpace(Options.AdminPassword);
    }

    private record UserDefinition(
        string Username,
        string Email,
        string FirstName,
        string LastName,
        string Password,
        params string[] Roles);

    private record KeycloakUser(
        string Username,
        string Email,
        string FirstName,
        string LastName,
        bool Enabled,
        List<KeycloakCredential> Credentials,
        List<string> RealmRoles
    );

    private record KeycloakCredential(string Type, string Value, bool Temporary);
}
