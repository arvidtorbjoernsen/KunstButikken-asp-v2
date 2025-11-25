using System.Reflection;

// Adds a lightweight GetEndpoint extension that calls a runtime GetEndpoint(string) method via reflection.
// Placing it in KunstButikken.ServiceDefaults keeps AppHost decoupled and avoids touching external types.

namespace KunstButikken.ServiceDefaults;

public static class ResourceBuilderExtensions
{
    /// <summary>
    ///     Try to resolve an endpoint name from a resource builder using reflection.
    ///     This is intentionally conservative and swallows errors because AppHost wiring should be non-fatal.
    /// </summary>
    public static string? GetEndpoint(this object? resource, string name)
    {
        if (resource is null)
        {
            return null;
        }

        try
        {
            if (resource is IResourceEndpointProvider provider)
            {
                return provider.GetEndpoint(name)?.ToString();
            }

            var type = resource.GetType();
            // Use proper Type[] overload for GetMethod
            var method = type.GetMethod("GetEndpoint", new Type[] { typeof(string) });
            if (method is null)
            {
                return null;
            }

            var value = method.Invoke(resource, new object[] { name });
            return value?.ToString();
        }
        catch (TargetInvocationException) // Catch specific exception for reflection invocation errors
        {
            return null;
        }
        catch (Exception) // Catch any other unexpected reflection-related exceptions
        {
            return null;
        }
    }

    /// <summary>
    ///     Returns a non-null string representation of an endpoint for use in environment variables.
    ///     Works both when the runtime exposes GetEndpoint returning EndpointReference and when using reflection.
    /// </summary>
    public static string GetEndpointString(this object? resource, string name)
    {
        if (resource is null)
        {
            return string.Empty;
        }

        try
        {
            if (resource is IResourceEndpointProvider provider)
            {
                return provider.GetEndpoint(name)?.ToString() ?? string.Empty;
            }

            var type = resource.GetType();
            var method = type.GetMethod("GetEndpoint", new Type[] { typeof(string) });
            if (method != null)
            {
                var value = method.Invoke(resource, new object[] { name });
                return value?.ToString() ?? string.Empty;
            }

            return GetEndpoint(resource, name) ?? string.Empty;
        }
        catch (TargetInvocationException) // Catch specific exception for reflection invocation errors
        {
            return string.Empty;
        }
        catch (Exception) // Catch any other unexpected reflection-related exceptions
        {
            return string.Empty;
        }
    }
}
