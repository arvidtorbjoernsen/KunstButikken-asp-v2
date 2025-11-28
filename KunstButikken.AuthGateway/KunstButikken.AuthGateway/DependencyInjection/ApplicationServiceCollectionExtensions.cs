using System.Net;
using KunstButikken.AuthGateway.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ServiceDiscovery;
using Yarp.ReverseProxy.Forwarder;

namespace KunstButikken.AuthGateway.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    private static readonly string[] DefaultClusters =
    {
        "admin-service-cluster",
        "art-service-cluster",
        "auction-service-cluster",
        "payment-service-cluster",
        "user-service-cluster"
    };

    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.Configure<ServiceDiscoveryOptions>(options => { options.AllowedSchemes = new[] { "http", "https" }; });

        services.AddReverseProxy().LoadFromConfig(configuration.GetSection("ReverseProxy"));
        services.AddSingleton<IForwarderHttpClientFactory, ServiceDiscoveryForwarderHttpClientFactory>();

        foreach (var clusterId in DefaultClusters)
        {
            services.AddHttpClient($"Yarp.Forwarder.{clusterId}")
                .AddServiceDiscovery()
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    AllowAutoRedirect = false,
                    AutomaticDecompression = DecompressionMethods.None,
                    ActivityHeadersPropagator = null,
                    ConnectTimeout = TimeSpan.FromSeconds(15)
                });
        }

        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

        return services;
    }
}
