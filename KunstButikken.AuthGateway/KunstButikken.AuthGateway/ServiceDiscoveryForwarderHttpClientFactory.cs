using Yarp.ReverseProxy.Forwarder;

namespace KunstButikken.AuthGateway;

/// <summary>
///     Custom forwarder HTTP client factory that integrates with Aspire service discovery
/// </summary>
public class ServiceDiscoveryForwarderHttpClientFactory(IHttpMessageHandlerFactory handlerFactory)
    : IForwarderHttpClientFactory
{
    public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // Create a unique name for each cluster to allow per-cluster configuration
        var clientName = $"Yarp.Forwarder.{context.ClusterId}";

        // Build the handler pipeline with service discovery
        var handler = handlerFactory.CreateHandler(clientName);

        // Create HttpMessageInvoker (not HttpClient) with the handler that includes service discovery
        return new HttpMessageInvoker(handler, true);
    }
}
