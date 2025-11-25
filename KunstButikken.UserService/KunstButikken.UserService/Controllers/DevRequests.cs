using System.Collections.ObjectModel;

namespace KunstButikken.UserService.Controllers;

public class SeedUsersRequest
{
    public bool Force { get; set; }
    public string? Realm { get; set; }
    // Expose as a getter-only collection so callers cannot replace the list (addresses CA2227).
    // System.Text.Json will populate the collection during deserialization.
    public Collection<string> Users { get; } = new Collection<string>();
}

public class SeedUsersResult
{
    public bool Allowed { get; set; }
    public string Realm { get; set; } = string.Empty;
    public Collection<string> Created { get; } = new Collection<string>();
    public Collection<string> Skipped { get; } = new Collection<string>();
    public Collection<string> Errors { get; } = new Collection<string>();
}

public class SyncUsersResult
{
    public bool Allowed { get; set; }
    public bool Triggered { get; set; }
    public string? LastRunError { get; set; }
    public bool? LastRunSucceeded { get; set; }
    public DateTimeOffset? LastRunUtc { get; set; }
    public TimeSpan? LastRunDuration { get; set; }
    public string? LastAuthTokenEndpoint { get; set; }
    public string? LastAuthTokenRealm { get; set; }
    public string? LastAuthClientId { get; set; }
    public string? LastAuthGrant { get; set; }
    public string? LastAuthHttpError { get; set; }
}
