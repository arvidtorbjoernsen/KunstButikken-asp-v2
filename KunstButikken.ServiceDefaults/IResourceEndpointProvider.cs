namespace KunstButikken.ServiceDefaults;

/// <summary>
///   Optional interface for runtime resource builders to expose a typed GetEndpoint method.
///   AppHost will prefer this interface if available to get endpoint strings in a type-safe way.
/// </summary>
public interface IResourceEndpointProvider
{
  /// <summary>
  ///   Return an endpoint reference or a string representation for the provided endpoint name.
  ///   Implementations may return any object; AppHost will call ToString() when necessary.
  /// </summary>
  object? GetEndpoint(string name);
}