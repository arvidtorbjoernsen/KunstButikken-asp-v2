// ...existing code...

using System;
using Xunit;
using KunstButikken.ServiceDefaults;

namespace KunstButikken.AppHost.Tests;

public class ResourceBuilderExtensionsTests
    public void GetEndpointString_UsesGetEndpointMethod_WhenAvailable()
    {
        var d = new DummyWithGetEndpoint();
        var s = KunstButikken.ServiceDefaults.ResourceBuilderExtensions.GetEndpointString(d, "web");
        Assert.Equal<string?>("https://example.com/web", s);
    }

    [Fact]
    public void GetEndpointString_UsesProvider_WhenAvailable()
    private class DummyWithGetEndpoint
    {
        public string GetEndpoint(string name) => $"https://example.com/{name}";
    }

    private class DummyProvider : KunstButikken.ServiceDefaults.IResourceEndpointProvider
    {
        public object? GetEndpoint(string name) => $"provider://{name}";
    }

    [Fact]
    public void GetEndpointString_ReturnsEmpty_ForNull()
    {
        string s = KunstButikken.ServiceDefaults.ResourceBuilderExtensions.GetEndpointString(null, "web");
        Assert.Equal<string?>(string.Empty, s);
    }

    [Fact]
        var p = new DummyProvider();
        var s = KunstButikken.ServiceDefaults.ResourceBuilderExtensions.GetEndpointString(p, "api");
        Assert.Equal<string?>("provider://api", s);
    {
        Assert.Equal("a","a");
    }
}
