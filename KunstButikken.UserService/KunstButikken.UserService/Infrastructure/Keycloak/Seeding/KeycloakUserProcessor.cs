using System.Collections;
using System.Text.Json;
using KunstButikken.Common.Logging;
using KunstButikken.UserService.Shared.Dev;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.UserService.Infrastructure.Keycloak.Seeding;

/// <summary>
///     Handles per-user operations against Keycloak (find/create/set-password/map-roles).
///     Extracted to keep the seeder focused and make testing easier.
/// </summary>
public sealed class KeycloakUserProcessor
{
    // Use centralized LoggerMessage delegates to keep this file small

    private readonly ILogger<KeycloakUserProcessor> _logger;

    public KeycloakUserProcessor(ILogger<KeycloakUserProcessor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ProcessUsersAsync(KeycloakUserManager manager, IEnumerable<dynamic> desired,
        SeedUsersResult result, CancellationToken ct)
    {
        if (manager is null)
        {
            throw new ArgumentNullException(nameof(manager));
        }

        if (desired is null)
        {
            throw new ArgumentNullException(nameof(desired));
        }

        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        foreach (var item in desired)
        {
            // Extract typed values up-front to avoid dynamic dispatch when calling extension methods
            string username = item?.Username?.ToString() ?? "(unknown)";
            IEnumerable<string> rolesEnumerable;
            try
            {
                if (item?.Roles is IEnumerable<string> sroles)
                {
                    rolesEnumerable = sroles;
                }
                else if (item?.Roles is IEnumerable oroles)
                {
                    var list = new List<string>();
                    foreach (var rn in oroles)
                    {
                        if (rn is null)
                        {
                            continue;
                        }

                        list.Add(rn.ToString()!);
                    }

                    rolesEnumerable = list;
                }
                else
                {
                    rolesEnumerable = Array.Empty<string>();
                }
            }
            catch
            {
                rolesEnumerable = Array.Empty<string>();
            }

            try
            {
                var userId = await manager.FindUserIdByUsernameAsync(username, ct).ConfigureAwait(false);

                if (userId == null)
                {
                    userId = await manager.CreateUserAsync(username, ct).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(userId))
                    {
                        result.Created.Add(username);
                        await manager.SetPasswordAsync(userId, username, ct).ConfigureAwait(false);
                    }
                }
                else
                {
                    result.Skipped.Add(username);
                }

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    var roleTuples = new List<(string id, string name)>();
                    foreach (var roleName in rolesEnumerable)
                    {
                        var role = await manager.GetRoleAsync(roleName, ct).ConfigureAwait(false);
                        if (role is not null)
                        {
                            roleTuples.Add(role.Value);
                        }
                    }

                    if (roleTuples.Count > 0)
                    {
                        var ok = await manager.MapRolesAsync(userId, roleTuples, ct).ConfigureAwait(false);
                        if (!ok)
                        {
                            result.Errors.Add($"Role mapping for {username} failed");
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                LogMessages.HttpRequestFailed(_logger, username, ex);
                result.Errors.Add($"{username}: HTTP request failed - {ex.Message}");
            }
            catch (JsonException ex)
            {
                LogMessages.JsonProcessingFailed(_logger, username, ex);
                result.Errors.Add($"{username}: JSON processing failed - {ex.Message}");
            }
            catch (OperationCanceledException ex)
            {
                LogMessages.OperationCanceled(_logger, username, ex);
                result.Errors.Add($"{username}: Operation canceled - {ex.Message}");
            }
            catch (DbUpdateException ex)
            {
                LogMessages.DbUpdateFailed(_logger, username, ex);
                result.Errors.Add($"{username}: Database update failed - {ex.Message}");
            }
            catch (Exception ex)
            {
                LogMessages.UnexpectedError(_logger, username, ex);
                result.Errors.Add($"{username}: Unexpected error - {ex.Message}");
            }
        }
    }
}
