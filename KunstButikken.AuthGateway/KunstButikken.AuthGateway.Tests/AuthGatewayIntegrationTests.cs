// KunstButikken.AuthGateway.Tests/AuthGatewayIntegrationTests.cs
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using System.Net;
using Xunit;

public class AuthGatewayIntegrationTests : IAsyncLifetime
{
    private IDistributedApplicationTestingBuilder _testingBuilder = null!;
    private DistributedApplication _app = null!;

    public async Task InitializeAsync()
    {
        // Boot the AppHost defined in your solution
        _testingBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.KunstButikken_AppHost>();

        _app = await _testingBuilder.BuildAsync();
        await _app.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _app.DisposeAsync();
        _testingBuilder.Dispose();
    }

    [Fact]
    public async Task Health_endpoint_returns_ok()
    {
        const string resourceName = "auth-gateway"; // <-- match AppHost name

        // Wait until the gateway is healthy to avoid race conditions
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await _app.ResourceNotifications.WaitForResourceHealthyAsync(resourceName, cts.Token);

        // Create an HttpClient for the resource's default "http" endpoint
        using var client = _app.CreateHttpClient(resourceName, endpointName: "http");

        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", body, StringComparison.OrdinalIgnoreCase);
    }
}
