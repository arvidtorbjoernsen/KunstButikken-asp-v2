using Xunit;
using KunstButikken.ServiceDefaults;

namespace KunstButikken.AppHost.Tests
{
    public class ResourceBuilderExtensionsTests
    {
        [Fact]
        public void GetEndpointString_UsesGetEndpointMethod_WhenAvailable()
        {
            var d = new DummyWithGetEndpoint();
            var s = ResourceBuilderExtensions.GetEndpointString(d, "web");
            Assert.Equal("https://example.com/web", s);
        }

        [Fact]
        public void GetEndpointString_UsesProvider_WhenAvailable()
        {
            var p = new DummyProvider();
            var s = ResourceBuilderExtensions.GetEndpointString(p, "api");
            Assert.Equal("provider://api", s);
        }

        [Fact]
        public void GetEndpointString_ReturnsEmpty_ForNull()
        {
            string s = ResourceBuilderExtensions.GetEndpointString(null, "web");
            Assert.Equal(string.Empty, s);
        }

        private class DummyWithGetEndpoint
        {
            public string GetEndpoint(string name) => $"https://example.com/{name}";
        }

        private class DummyProvider : IResourceEndpointProvider
        {
            public object GetEndpoint(string name) => $"provider://{name}";
        }
    }
}
