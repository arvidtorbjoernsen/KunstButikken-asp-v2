using System.Reflection;

namespace KunstButikken.ServiceDefaults;

public static class WaitForHttpExtensions
{
    /// <summary>
    ///     Convenience helper that sets a runtime READINESS_PATH env var on a resource and returns the resource for chaining.
    ///     It uses the existing GetEndpoint(resource, name) helper to resolve a runtime endpoint reference.
    ///     This method is intentionally conservative and uses reflection so it works with multiple runtime builder types.
    /// </summary>
    public static TResource? WaitForHttp<TResource>(this TResource? resource,
        string readinessPath, // Changed TResource to TResource?
        string endpointName = "api") where TResource : class
    {
        if (resource is null)
        {
            return resource;
        }

        try
        {
            // Resolve a runtime endpoint reference (may be EndpointReference or plain string)
            var ep = resource.GetEndpoint(endpointName)
                     ?? resource.GetEndpoint("gateway")
                     ?? resource.GetEndpoint("web");

            var readiness = (ep ?? string.Empty) + readinessPath;

            // Try to call WithEnvironment(string, object) or WithEnvironment(string, string)
            var type = resource.GetType();
            var method = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                    m.Name == "WithEnvironment" && m.GetParameters().Length == 2 &&
                    m.GetParameters()[0].ParameterType == typeof(string));

            if (method != null)
            {
                var second = method.GetParameters()[1].ParameterType;
                object arg2 = (object)readiness;

                method.Invoke(resource, new object[] { "READINESS_PATH", arg2 });
            }
        }
        catch (TargetInvocationException) // Catch specific exception for reflection invocation errors
        {
            // Swallow all errors as this is a best-effort convenience helper for AppHost wiring
        }
        catch (Exception) // Catch any other unexpected reflection-related exceptions
        {
            // Swallow all errors as this is a best-effort convenience helper for AppHost wiring
        }

        return resource;
    }
}
