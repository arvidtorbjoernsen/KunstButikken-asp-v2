using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace KunstButikken.AppHost.Tests;

public class PropagateCorsTests
{
    private class FakeEndpointsBuilder : object // will be used as IResourceBuilder<IResourceWithEndpoints>
    {
        public string? Name { get; set; }
        public string? Endpoint { get; set; }
        public object GetEndpoint(string name) => Endpoint ?? string.Empty;
    }

    private class FakeEnvBuilder
    {
        public readonly Dictionary<string, string> Env = new Dictionary<string, string>();
        public FakeEnvBuilder WithEnvironment(string key, object? value)
        {
            Env[key] = value?.ToString() ?? string.Empty;
            return this;
        }
    }

    [Fact]
    public void PropagateCors_SetsFrontendOriginsOnServices()
    {
        var frontends = new List<FakeEndpointsBuilder>
        {
            new FakeEndpointsBuilder { Endpoint = "https://a.example.com" },
            new FakeEndpointsBuilder { Endpoint = "https://b.example.com" }
        };

        var svc1 = new FakeEnvBuilder();
        var svc2 = new FakeEnvBuilder();
        var services = new List<FakeEnvBuilder> { svc1, svc2 };

        // Call the object-based overload which we added to AppHostHelpers
        AppHostHelpers.PropagateCors(frontends.Cast<object>(), services.Cast<object>());

        Assert.True(svc1.Env.TryGetValue("FRONTEND_ORIGINS", out var v1));
        Assert.Contains("https://a.example.com", v1);
        Assert.Contains("https://b.example.com", v1);

        Assert.True(svc2.Env.TryGetValue("FRONTEND_ORIGINS", out var v2));
        Assert.Equal(v1, v2);
    }
}
