using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Xunit;

namespace KunstButikken.AppHost.Tests;

public sealed class AspireSmokeTests
{
    [Fact]
    public async Task ArtService_ShouldExposePublicListing()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync();
        var artService = builder.AddProject("art-service", "../../KunstButikken.ArtService/KunstButikken.ArtService/KunstButikken.ArtService.csproj")
            .WithHttpEndpoint(name: "api");

        await using var app = await builder.BuildAsync();
        var http = app.CreateHttpClient(artService, "api");
        var response = await http.GetAsync("/api/art");
        response.EnsureSuccessStatusCode();
    }
}
