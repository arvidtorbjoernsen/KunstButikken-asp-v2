using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KunstButikken.AppHost.Tests;

public class AppHostHelpersMoreTests
{
    private class FakeEnvBuilder
    {
        public readonly Dictionary<string, string> Env = new Dictionary<string, string>();
        public FakeEnvBuilder WithEnvironment(string key, object? value)
        {
            Env[key] = value?.ToString() ?? string.Empty;
            return this;
        }
    }

    private class ThrowingFrontend
    {
        public object GetEndpoint(string name) => throw new InvalidOperationException("boom");
    }

    private class FakeFrontend
    {
        private readonly string _endpoint;
        public FakeFrontend(string endpoint) => _endpoint = endpoint;
        public string GetEndpoint(string name) => _endpoint;
    }

    private class FakeBuilderWithNoServices { }

    private class FakeBuilderWithNullServices
    {
        public object? Services => null;
    }

    private class FakeBuilderWithInvalidServices
    {
        public object Services => "not a service collection";
    }

    private class FakeBuilderWithServices
    {
        public IServiceCollection Services { get; } = new ServiceCollection();
    }

    private class FakeEnvOnlyBuilder
    {
        public IDictionary<string, string> Env { get; } = new Dictionary<string, string>();
    }

    private class FakeEnvBuilderWithEnvProperty
    {
        public IDictionary<string, string> Env { get; } = new Dictionary<string, string>();
        public FakeEnvBuilderWithEnvProperty WithEnvironment(string key, object? value)
        {
            throw new InvalidOperationException("boom");
        }
    }


    [Fact]
    public void PropagateCors_WithEmptyFrontends_SetsEmptyEnvOnServices()
    {
        var frontends = new List<object>();
        var svc = new FakeEnvBuilder();
        var services = new List<object> { svc };

        // Should not throw
        AppHostHelpers.PropagateCors(frontends, services);

        Assert.True(svc.Env.ContainsKey("FRONTEND_ORIGINS"));
        Assert.Equal(string.Empty, svc.Env["FRONTEND_ORIGINS"]);
    }

    [Fact]
    public void PropagateCors_WithThrowingFrontend_DoesNotThrowAndSetsEmptyEnv()
    {
        var frontends = new List<object> { new ThrowingFrontend() };
        var svc = new FakeEnvBuilder();
        var services = new List<object> { svc };

        // Should not throw even if GetEndpoint throws
        AppHostHelpers.PropagateCors(frontends, services);

        Assert.True(svc.Env.ContainsKey("FRONTEND_ORIGINS"));
        Assert.Equal(string.Empty, svc.Env["FRONTEND_ORIGINS"]);
    }

    [Fact]
    public void PropagateCors_WithValidFrontendsAndServices_SetsCorrectEnvOnServices()
    {
        var frontends = new List<object>
        {
            new FakeFrontend("http://localhost:3000"),
            new FakeFrontend("http://localhost:4200")
        };
        var svc = new FakeEnvBuilder();
        var services = new List<object> { svc };

        AppHostHelpers.PropagateCors(frontends, services);

        Assert.True(svc.Env.ContainsKey("FRONTEND_ORIGINS"));
        Assert.Equal("http://localhost:3000,http://localhost:4200", svc.Env["FRONTEND_ORIGINS"]);
    }

    [Fact]
    public void PropagateCors_DeduplicatesFrontendOrigins()
    {
        var frontends = new List<object>
        {
            new FakeFrontend("https://example.com"),
            new FakeFrontend("https://example.com"),
            new FakeFrontend("https://other.com")
        };
        var svc = new FakeEnvBuilder();
        var services = new List<object> { svc };

        AppHostHelpers.PropagateCors(frontends, services);

        Assert.Equal("https://example.com,https://other.com", svc.Env["FRONTEND_ORIGINS"]);
    }

    [Fact]
    public void PropagateCors_IgnoresNullServices()
    {
        var frontends = new List<object> { new FakeFrontend("https://null.test") };
        var svc = new FakeEnvBuilder();
        var services = new object?[] { null, svc };

        AppHostHelpers.PropagateCors(frontends, services);

        Assert.Equal("https://null.test", svc.Env["FRONTEND_ORIGINS"]);
    }

    [Fact]
    public void PropagateCors_WithEnvOnlyService_UpdatesDictionary()
    {
        var frontends = new List<object> { new FakeFrontend("https://envonly.test") };
        var svc = new FakeEnvOnlyBuilder();

        AppHostHelpers.PropagateCors(frontends, new object[] { svc });

        Assert.Equal("https://envonly.test", svc.Env["FRONTEND_ORIGINS"]);
    }

    [Fact]
    public void PropagateCors_WithThrowingWithEnvironment_FallsBackToEnvProperty()
    {
        var frontends = new List<object> { new FakeFrontend("https://fallback.test") };
        var svc = new FakeEnvBuilderWithEnvProperty();

        AppHostHelpers.PropagateCors(frontends, new object[] { svc });

        Assert.Equal("https://fallback.test", svc.Env["FRONTEND_ORIGINS"]);
    }

    [Fact]
    public void FindRepoRoot_ReturnsNonEmptyPath()
    {
        var orig = Environment.GetEnvironmentVariable("REPO_ROOT");
        try
        {
            Environment.SetEnvironmentVariable("REPO_ROOT", null);
            var result = AppHostHelpers.FindRepoRoot();
            Assert.False(string.IsNullOrWhiteSpace(result));
        }
        finally
        {
            Environment.SetEnvironmentVariable("REPO_ROOT", orig);
        }
    }

    [Fact]
    public void FindRepoRoot_WithRepoRootEnvVar_ReturnsEnvVarValue()
    {
        var orig = Environment.GetEnvironmentVariable("REPO_ROOT");
        try
        {
            var expected = "/fake/repo/root";
            Environment.SetEnvironmentVariable("REPO_ROOT", expected);
            var result = AppHostHelpers.FindRepoRoot();
            Assert.Equal(expected, result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("REPO_ROOT", orig);
        }
    }

    [Fact]
    public void TryRegisterMcpServer_WithNullBuilder_DoesNotThrow()
    {
        // Should not throw
        AppHostHelpers.TryRegisterMcpServer(null);
    }

    [Fact]
    public void TryRegisterMcpServer_WithBuilderWithoutServices_DoesNotThrow()
    {
        // Should not throw
        AppHostHelpers.TryRegisterMcpServer(new FakeBuilderWithNoServices());
    }

    [Fact]
    public void TryRegisterMcpServer_WithBuilderWithNullServices_DoesNotThrow()
    {
        // Should not throw
        AppHostHelpers.TryRegisterMcpServer(new FakeBuilderWithNullServices());
    }

    [Fact]
    public void TryRegisterMcpServer_WithBuilderWithInvalidServices_DoesNotThrow()
    {
        // Should not throw
        AppHostHelpers.TryRegisterMcpServer(new FakeBuilderWithInvalidServices());
    }

    [Fact]
    public void TryRegisterMcpServer_WithValidBuilder_DoesNotThrow()
    {
        // Should not throw, even though MCP registration will fail internally
        AppHostHelpers.TryRegisterMcpServer(new FakeBuilderWithServices());
    }
}
