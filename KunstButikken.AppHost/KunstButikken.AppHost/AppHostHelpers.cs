namespace KunstButikken.AppHost;

using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

internal static class AppHostHelpers
{
    // Find repository root: prefer assembly metadata emitted from Directory.Build.props, then REPO_ROOT env, then small upward search.
    [SuppressMessage("SonarAnalyzer.CSharp", "S3776", Justification = "Small helper with simple control flow; acceptable for now")]
    public static string FindRepoRoot()
    {
        try
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var repoMeta = asm.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => string.Equals(a.Key, "RepoRoot", StringComparison.OrdinalIgnoreCase))?.Value;
            if (!string.IsNullOrWhiteSpace(repoMeta))
            {
                return repoMeta;
            }

            var env = Environment.GetEnvironmentVariable("REPO_ROOT");
            if (!string.IsNullOrWhiteSpace(env))
            {
                return env;
            }

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (var i = 0; i < 6 && dir != null; i++)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")) ||
                    string.Equals(dir.Name, "KunstButikken-asp", StringComparison.OrdinalIgnoreCase))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }

            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", ".."));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AppHostHelpers] FindRepoRoot failed: {ex.Message}");
            return AppContext.BaseDirectory;
        }
    }

    // High level entry: accept any builder object and try to register MCP server services
    public static void TryRegisterMcpServer(object? builder)
    {
        try
        {
            if (builder == null)
            {
                Console.WriteLine("[AppHostHelpers] builder is null; skipping MCP registration.");
                return;
            }

            var servicesProp = builder.GetType().GetProperty("Services");
            if (servicesProp == null)
            {
                Console.WriteLine("[AppHostHelpers] builder.Services property not found; skipping MCP registration.");
                return;
            }

            var services = servicesProp.GetValue(builder) as IServiceCollection;
            if (services == null)
            {
                Console.WriteLine("[AppHostHelpers] builder.Services is null or not IServiceCollection; skipping MCP registration.");
                return;
            }

            // Try strong-typed first
            if (TryRegisterMcpServer_StrongTyped(services))
            {
                return;
            }

            // Otherwise try reflective registration
            if (TryRegisterMcpServer_Reflective(builder, services))
            {
                return;
            }

            Console.WriteLine("[AppHostHelpers] No MCP registration found. To enable MCP add the ModelContextProtocol packages to AppHost project.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AppHostHelpers] TryRegisterMcpServer failed: {ex.Message}");
        }
    }

    // Try compile-time strong-typed registration; return true when successful
    private static bool TryRegisterMcpServer_StrongTyped(IServiceCollection services)
    {
        try
        {
            // Use overload without explicit null argument to avoid analyzer warning about redundant default param
            var mcpBuilder = McpServerServiceCollectionExtensions.AddMcpServer(services);
            try
            {
                HttpMcpServerBuilderExtensions.AddAuthorizationFilters(mcpBuilder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppHostHelpers] AddAuthorizationFilters warning: {ex.Message}");
            }
            Console.WriteLine("[AppHostHelpers] Registered MCP server via AddMcpServer().");
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Reflective probe registration; return true if any invocation succeeded
    [SuppressMessage("SonarAnalyzer.CSharp", "S3776", Justification = "Reflective discovery is inherently a bit complex; suppress while functionality is validated")]
    private static bool TryRegisterMcpServer_Reflective(object builder, IServiceCollection services)
    {
        var candidateNames = new[] { "ModelContextProtocol.AspNetCore", "ModelContextProtocol", "ModelContextProtocol.Core", "ModelContextProtocol.Server", "ModelContextProtocol.AspNetCore.Server" };
        foreach (var name in candidateNames)
        {
            try
            {
                var asm = Assembly.Load(new AssemblyName(name));
                var types = asm.GetExportedTypes();
                foreach (var t in types)
                {
                    if (!t.IsSealed || !t.IsAbstract)
                    {
                        continue;
                    }

                    var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static);
                    foreach (var m in methods)
                    {
                        var ps = m.GetParameters();
                        try
                        {
#pragma warning disable CS8604,CS8600,CS8625
                            if (ps.Length == 2 && ps[0].ParameterType.Name.Contains("IServiceCollection", StringComparison.OrdinalIgnoreCase))
                            {
                                m.Invoke(null, new object[] { services, null });
                                Console.WriteLine($"[AppHostHelpers] Invoked {m.Name} on {name} (IServiceCollection signature)");
                                return true;
                            }
                            if (ps.Length == 1 && ps[0].ParameterType.Name.Contains("IServiceCollection", StringComparison.OrdinalIgnoreCase))
                            {
                                m.Invoke(null, new object[] { services });
                                Console.WriteLine($"[AppHostHelpers] Invoked {m.Name} on {name} (IServiceCollection single)");
                                return true;
                            }
                            if (ps.Length >= 1 && ps[0].ParameterType.Name.Contains("DistributedApplication", StringComparison.OrdinalIgnoreCase))
                            {
                                object[] args;
                                if (ps.Length == 1)
                                {
                                    args = new object[] { builder };
                                }
                                else
                                {
                                    args = new object[] { builder, null };
                                }
                                m.Invoke(null, args);
                                Console.WriteLine($"[AppHostHelpers] Invoked {m.Name} on {name} (DistributedApplication-like signature)");
                                return true;
                            }
#pragma warning restore CS8604,CS8600,CS8625
                        }
                        catch (Exception invokeEx)
                        {
                            Console.WriteLine($"[AppHostHelpers] Invocation failed for {m.Name}: {invokeEx.Message}");
                        }
                    }
                }
            }
            catch
            {
                // ignore assembly load failures
            }
        }

        return false;
    }

    public static void PropagateCors(
        IEnumerable<IResourceBuilder<IResourceWithEndpoints>> frontends,
        IEnumerable<IResourceBuilder<IResourceWithEnvironment>> services)
    {
        try
        {
            var origins = frontends
                .Select(f => KunstButikken.ServiceDefaults.ResourceBuilderExtensions.GetEndpointString(f, "web"))
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToArray();
            foreach (var svc in services)
            {
                svc.WithEnvironment("FRONTEND_ORIGINS", string.Join(",", origins));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AppHostHelpers] PropagateCors failed: {ex.Message}");
        }
    }
}
