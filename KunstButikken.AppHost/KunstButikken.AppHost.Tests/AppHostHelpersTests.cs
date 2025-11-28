// ...existing code...

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KunstButikken.AppHost.Tests;

public class AppHostHelpersTests
{
    [Fact]
    public void FindRepoRoot_UsesEnvVar()
    {
        var orig = Environment.GetEnvironmentVariable("REPO_ROOT");
        try
        {
            Environment.SetEnvironmentVariable("REPO_ROOT", "/tmp/myrepo");
            var result = KunstButikken.AppHost.AppHostHelpers.FindRepoRoot();
            Assert.Equal<string?>("/tmp/myrepo", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("REPO_ROOT", orig);
        }
    }

    private class BuilderNoServices { }

    private class BuilderWithServices
    {
        public Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; } = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
    }

    [Fact]
    public void TryRegisterMcpServer_DoesNotThrow_ForMissingServicesProp()
    {
        var b = new BuilderNoServices();
        KunstButikken.AppHost.AppHostHelpers.TryRegisterMcpServer(b);
        // success if no exception
    }

    [Fact]
    public void TryRegisterMcpServer_DoesNotThrow_ForNullServicesProperty()
    {
        var b = new { Services = (Microsoft.Extensions.DependencyInjection.IServiceCollection?)null };
        KunstButikken.AppHost.AppHostHelpers.TryRegisterMcpServer(b);
    }

    [Fact]
    public void TryRegisterMcpServer_DoesNotThrow_ForIServiceCollection()
    {
        var b = new BuilderWithServices();
        KunstButikken.AppHost.AppHostHelpers.TryRegisterMcpServer(b);
    }
}
