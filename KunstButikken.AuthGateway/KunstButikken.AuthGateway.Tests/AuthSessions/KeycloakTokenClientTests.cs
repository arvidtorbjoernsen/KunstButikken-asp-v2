using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using KunstButikken.AuthGateway.Application.AuthSessions;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;

namespace KunstButikken.AuthGateway.Tests.AuthSessions;

public class KeycloakTokenClientTests
{
    [Fact]
    public async Task ExchangeCodeAsync_ReturnsNullOnFailure()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(nameof(KeycloakTokenClient))).Returns(new HttpClient(handler.Object));

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["KEYCLOAK_TOKEN_ENDPOINT"] = "https://idp/token"
        }).Build();

        var client = new KeycloakTokenClient(factory.Object, configuration);
        var result = await client.ExchangeCodeAsync("code", "https://app/callback");

        Assert.Null(result);
    }
}

