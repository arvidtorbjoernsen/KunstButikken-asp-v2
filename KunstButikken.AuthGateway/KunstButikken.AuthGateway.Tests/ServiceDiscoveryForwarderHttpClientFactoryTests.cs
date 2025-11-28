using System.Net.Http;
using FluentAssertions;
using KunstButikken.AuthGateway;
using KunstButikken.AuthGateway.Services;
using Microsoft.Extensions.Http;
using Moq;
using Yarp.ReverseProxy.Forwarder;

namespace KunstButikken.AuthGateway.Tests;

public class ServiceDiscoveryForwarderHttpClientFactoryTests
{
    [Fact]
    public void CreateClient_UsesClusterSpecificHandler()
    {
        var handlerFactory = new Mock<IHttpMessageHandlerFactory>();
        var handler = new HttpClientHandler();
        handlerFactory.Setup(h => h.CreateHandler("Yarp.Forwarder.test-cluster"))
            .Returns(handler);

        var factory = new ServiceDiscoveryForwarderHttpClientFactory(handlerFactory.Object);
        var context = new ForwarderHttpClientContext
        {
            ClusterId = "test-cluster"
        };

        var invoker = factory.CreateClient(context);

        invoker.Should().NotBeNull();
    }
}
