using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

// Re-enable OpenTelemetry usings so we can configure it when the packages are present
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace KunstButikken.ServiceDefaults;

// Adds common Aspire services: service discovery, resilience, health checks, and telemetry.
// This project should be referenced by each service project in your solution.
// Telemetry setup will prefer Aspire helpers when present; otherwise it will configure OpenTelemetry
// from the restored OpenTelemetry packages so behaviour is consistent with previous setup.
public static class ServiceDefaultsExtensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureTelemetryAspireFirstOrOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// Try to configure telemetry using Aspire's helpers if available; otherwise configure OpenTelemetry.
    /// </summary>
    public static TBuilder ConfigureTelemetryAspireFirstOrOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        // If an Aspire-provided AddServiceDefaults(IHostApplicationBuilder) extension exists in a different assembly,
        // prefer invoking that so Aspire's full defaults are applied.
        try
        {
            var currentAsm = typeof(ServiceDefaultsExtensions).Assembly;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm == currentAsm) continue; // skip our own assembly
                try
                {
                    var types = asm.GetExportedTypes();
                    foreach (var t in types)
                    {
                        if (!t.IsSealed || !t.IsAbstract) continue; // static classes only
                        var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static)
                            .Where(m => string.Equals(m.Name, "AddServiceDefaults", StringComparison.Ordinal));
                        foreach (var m in methods)
                        {
                            var ps = m.GetParameters();
                            if (ps.Length == 1 && ps[0].ParameterType.IsAssignableFrom(typeof(IHostApplicationBuilder)))
                            {
                                try
                                {
                                    m.Invoke(null, new object[] { builder });
                                    Console.WriteLine("[ServiceDefaults] Invoked Aspire AddServiceDefaults from " + asm.GetName().Name + ".");
                                    return builder;
                                }
                                catch (TargetInvocationException tie)
                                {
                                    Console.WriteLine("[ServiceDefaults] Aspire AddServiceDefaults invocation failed: " + (tie.InnerException?.Message ?? tie.Message));
                                    // If invoke fails try other candidates
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // ignore assembly inspection failures
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ServiceDefaults] Aspire AddServiceDefaults probe failed: " + ex.Message);
        }

        // First try Aspire helpers via reflection
        try
        {
            var candidateTypes = new[]
            {
                "Aspire.Hosting.Telemetry.TelemetryExtensions",
                "Aspire.Hosting.Telemetry.HostingTelemetryExtensions",
                "Aspire.Hosting.TelemetryExtensions",
                "Aspire.Hosting.Telemetry.HostingExtensions"
            };

            foreach (var typeName in candidateTypes)
            {
                try
                {
                    var asm = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.GetType(typeName, false) != null);
                    if (asm == null) continue;

                    var t = asm.GetType(typeName, false);
                    if (t == null) continue;

                    var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static);
                    foreach (var m in methods)
                    {
                        var ps = m.GetParameters();

                        if (ps.Length == 1 && ps[0].ParameterType.IsAssignableFrom(typeof(IHostApplicationBuilder)))
                        {
                            m.Invoke(null, new object[] { builder });
                            Console.WriteLine("[ServiceDefaults] Configured telemetry via " + typeName + ".");
                            return builder;
                        }

                        if (ps.Length == 1 && ps[0].ParameterType.IsAssignableFrom(typeof(IServiceCollection)))
                        {
                            m.Invoke(null, new object[] { builder.Services });
                            Console.WriteLine("[ServiceDefaults] Configured telemetry via " + typeName + "(IServiceCollection)." );
                            return builder;
                        }

                        if (ps.Length == 2 && ps[0].ParameterType.IsAssignableFrom(typeof(IServiceCollection)) && ps[1].ParameterType.IsAssignableFrom(typeof(ILoggingBuilder)))
                        {
                            m.Invoke(null, new object[] { builder.Services, builder.Logging });
                            Console.WriteLine("[ServiceDefaults] Configured telemetry via " + typeName + "(IServiceCollection, ILoggingBuilder)." );
                            return builder;
                        }
                    }
                }
                catch (TargetInvocationException tie)
                {
                    Console.WriteLine("[ServiceDefaults] Aspire telemetry helper invocation failed: " + (tie.InnerException?.Message ?? tie.Message));
                }
                catch
                {
                    // ignore and try next
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ServiceDefaults] Aspire telemetry aspirational configuration failed: " + ex.Message);
        }

        // If we got here, fall back to configuring OpenTelemetry using the project's packages
        try
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation();
                })
                .WithTracing(tracing =>
                {
                    tracing.AddSource(builder.Environment.ApplicationName)
                        .AddAspNetCoreInstrumentation(tracing =>
                            tracing.Filter = context =>
                                !context.Request.Path.StartsWithSegments(HealthEndpointPath, StringComparison.OrdinalIgnoreCase)
                                && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath, StringComparison.OrdinalIgnoreCase)
                        )
                        .AddHttpClientInstrumentation();
                });

            // Configure exporters based on environment
            var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
            if (useOtlpExporter)
            {
                builder.Services.AddOpenTelemetry().UseOtlpExporter();
            }

            Console.WriteLine("[ServiceDefaults] Configured OpenTelemetry telemetry (fallback).");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ServiceDefaults] OpenTelemetry configuration failed: " + ex.Message);
            // As a final fallback enable console logging so the app is still useful
            try { builder.Logging.AddConsole(); } catch { }
        }

        return builder;
    }

    private static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "live" });

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}
