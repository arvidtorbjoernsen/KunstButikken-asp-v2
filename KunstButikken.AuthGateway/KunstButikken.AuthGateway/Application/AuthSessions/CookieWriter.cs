using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace KunstButikken.AuthGateway.Application.AuthSessions;

public interface IAuthCookieWriter
{
    void WriteSessionCookie(HttpResponse response, AuthSession session);
    void WriteRefreshCookie(HttpResponse response, AuthSession session);
    void ClearCookies(HttpResponse response);
}

public sealed class CookieWriter(IOptions<AuthSessionOptions> options) : IAuthCookieWriter
{
    private readonly AuthSessionOptions _options = options.Value;

    public void WriteSessionCookie(HttpResponse response, AuthSession session)
    {
        WriteCookie(response, _options.SessionCookieName, session.SessionId, session.ExpiresAt, _options.CookieSameSite);
    }

    public void WriteRefreshCookie(HttpResponse response, AuthSession session)
    {
        WriteCookie(response, _options.RefreshCookieName, session.RefreshToken, session.RefreshExpiresAt, _options.RefreshSameSite);
    }

    public void ClearCookies(HttpResponse response)
    {
        response.Cookies.Delete(_options.SessionCookieName);
        response.Cookies.Delete(_options.RefreshCookieName);
    }

    private void WriteCookie(HttpResponse response, string name, string value, DateTimeOffset expiresAt, string sameSite)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = _options.CookieSecure,
            SameSite = ParseSameSite(sameSite),
            Path = "/",
            Expires = expiresAt,
        };

        if (!string.IsNullOrWhiteSpace(_options.CookieDomain))
        {
            options.Domain = _options.CookieDomain;
        }

        response.Cookies.Append(name, value, options);
    }

    private static SameSiteMode ParseSameSite(string value) => value?.ToLowerInvariant() switch
    {
        "none" => SameSiteMode.None,
        "lax" => SameSiteMode.Lax,
        "strict" => SameSiteMode.Strict,
        _ => SameSiteMode.Lax,
    };
}

