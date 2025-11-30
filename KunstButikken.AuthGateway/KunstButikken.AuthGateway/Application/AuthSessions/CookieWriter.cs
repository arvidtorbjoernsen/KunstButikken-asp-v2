using KunstButikken.AuthGateway.Domain;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace KunstButikken.AuthGateway.Application.AuthSessions;

public interface IAuthCookieWriter
{
    void WriteSessionCookie(HttpResponse response, AuthSession session);
    void WriteRefreshCookie(HttpResponse response, AuthSession session);
    void WriteAccessTokenCookie(HttpResponse response, AuthSession session);
    void ClearCookies(HttpResponse response);
}

public sealed class CookieWriter(
    IOptions<AuthSessionOptions> options
) : IAuthCookieWriter
{
    private readonly AuthSessionOptions _options = options.Value;

    public void WriteSessionCookie(HttpResponse response, AuthSession session)
    {
        WriteCookie(response, _options.SessionCookieName, session.SessionId, session.ExpiresAt, _options.CookieSameSite);
        WriteAccessTokenCookie(response, session);
    }

    public void WriteRefreshCookie(HttpResponse response, AuthSession session) =>
        WriteCookie(response, _options.RefreshCookieName, session.RefreshToken, session.RefreshExpiresAt, _options.RefreshSameSite);

    public void WriteAccessTokenCookie(HttpResponse response, AuthSession session)
    {
        if (string.IsNullOrEmpty(_options.AccessTokenCookieName))
        {
            return;
        }

        WriteCookie(
            response,
            _options.AccessTokenCookieName,
            session.AccessToken,
            session.ExpiresAt,
            _options.CookieSameSite,
            httpOnly: false,
            sameSiteOverride: "Lax"
        );
    }

    public void ClearCookies(HttpResponse response)
    {
        response.Cookies.Delete(_options.SessionCookieName);
        response.Cookies.Delete(_options.RefreshCookieName);
        if (!string.IsNullOrEmpty(_options.AccessTokenCookieName))
        {
            response.Cookies.Delete(_options.AccessTokenCookieName);
        }
    }

    private void WriteCookie(HttpResponse response, string name, string value, DateTimeOffset expiresAt, string sameSite, bool httpOnly = true, string? sameSiteOverride = null)
    {
        var options = new CookieOptions
        {
            HttpOnly = httpOnly,
            Secure = _options.CookieSecure,
            SameSite = ParseSameSite(sameSiteOverride ?? sameSite),
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
        _ => SameSiteMode.Lax
    };
}
