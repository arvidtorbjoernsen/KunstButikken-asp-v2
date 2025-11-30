using KunstButikken.AuthGateway.Application.AuthSessions;
using KunstButikken.AuthGateway.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace KunstButikken.AuthGateway.Tests.AuthSessions;

public class CookieWriterTests
{
    [Fact]
    public void WritesBothCookies()
    {
        var options = Options.Create(new AuthSessionOptions { CookieSecure = false, CookieDomain = "" });
        var writer = new CookieWriter(options);
        var response = new DefaultHttpContext().Response;
        var session = new AuthSession("sid", "refresh", "user", null, null, null, Array.Empty<string>(), "access", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5), DateTimeOffset.UtcNow.AddHours(1));

        writer.WriteSessionCookie(response, session);
        writer.WriteRefreshCookie(response, session);

        Assert.Contains(AuthSessionOptions.SessionCookie, response.Headers["Set-Cookie"].ToString());
        Assert.Contains(AuthSessionOptions.RefreshCookie, response.Headers["Set-Cookie"].ToString());
    }
}
