using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using FluentAssertions;
using KunstButikken.AppHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace KunstButikken.AuthGateway.Tests;

public sealed class AuthGatewayIntegrationTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        var testingBuilder = AppHost.AppHostExtensions.CreateTestingBuilder();
        var composition = AppHostProjects.BuildTestingApplication(testingBuilder);
        var authGatewayName = composition.AuthGateway.Resource.Name;
        testingBuilder.WithApp(app => app.WithEntrypoint(authGatewayName));

        _app = await testingBuilder.BuildAsync();
        await _app.StartAsync();
        _client = _app.CreateHttpClient(authGatewayName);
    }

    public async Task DisposeAsync()
    {
        if (_client is not null)
        {
            _client.Dispose();
        }
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task Alive_endpoint_returns_ok()
    {
        var response = await _client!.GetAsync("/auth/alive");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AliveResponse>();
        payload.Should().NotBeNull();
        payload!.status.Should().Be("ok");
    }

    private sealed record AliveResponse(string status, string service, DateTimeOffset time);
}
