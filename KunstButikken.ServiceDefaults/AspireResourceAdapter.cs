using System.Reflection;

namespace KunstButikken.ServiceDefaults;

/// <summary>
///     Adapter that exposes an arbitrary runtime resource builder as an <see cref="IResourceEndpointProvider" />.
///     Useful when the runtime types cannot implement the interface directly.
/// </summary>
public sealed class AspireResourceAdapter(object inner) : IResourceEndpointProvider
{
    private readonly object _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    public object? GetEndpoint(string name)
    {
        if (_inner is IResourceEndpointProvider p)
        {
            return p.GetEndpoint(name);
        }

        // Try a method first
        try
        {
            var t = _inner.GetType();
            var method = t.GetMethod("GetEndpoint", [typeof(string)]);
            if (method != null)
            {
                return method.Invoke(_inner, [name]);
            }

            // If there's a property bag for endpoints, try common names
            var prop = t.GetProperty("Endpoints") ?? t.GetProperty("Endpoint") ?? null;
            if (prop != null)
            {
                var value = prop.GetValue(_inner);
                // If it's a dictionary-like type, try to fetch
                var dictTry = value?.GetType()
                    .GetMethod("TryGetValue", [typeof(string), typeof(object).MakeByRefType()]);
                if (dictTry != null)
                {
                    var args = new object[]
                    {
                        name, null!
                    };
                    var ok = (bool)dictTry.Invoke(value, args)!;
                    if (ok)
                    {
                        return args[1];
                    }
                }

                return value?.ToString();
            }
        }
        catch (TargetInvocationException) // Catch specific exception for reflection invocation errors
        {
            // Swallow and return null — adapter is conservative
        }
        catch (Exception) // Catch any other unexpected reflection-related exceptions
        {
            // Swallow and return null — adapter is conservative
        }

        return null;
    }
}

public static class AspireResourceAdapterExtensions
{
    /// <summary>
    ///     Return an <see cref="IResourceEndpointProvider" /> for the provided resource.
    ///     If the resource already implements the interface it is returned as-is; otherwise an adapter is created.
    /// </summary>
    public static IResourceEndpointProvider AsResourceEndpointProvider(this object? resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        if (resource is IResourceEndpointProvider p)
        {
            return p;
        }

        return new AspireResourceAdapter(resource);
    }
}
