using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace KunstButikken.ServiceDefaults.Tests;

public class ServiceDefaultsExtensionsTests
{
    [Fact]
    public void AddServiceDefaults_RegistersHealthCheckService()
    {
        var builder = new HostApplicationBuilder();

        var result = builder.AddServiceDefaults();

        Assert.Same(builder, result);
        Assert.Contains(builder.Services, descriptor => descriptor.ServiceType == typeof(HealthCheckService));
    }

    [Fact]
    public void ConfigureTelemetryAspireFirstOrOpenTelemetry_AddsOpenTelemetryProviders()
    {
        var builder = new HostApplicationBuilder();

        var result = builder.ConfigureTelemetryAspireFirstOrOpenTelemetry();

        Assert.Same(builder, result);
        Assert.Contains(builder.Services, descriptor => descriptor.ServiceType?.FullName == "OpenTelemetry.Trace.TracerProvider");
        Assert.Contains(builder.Services, descriptor => descriptor.ServiceType?.FullName == "OpenTelemetry.Metrics.MeterProvider");
    }
}
