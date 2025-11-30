namespace KunstButikken.AuthGateway.Application.AuthSessions;

public sealed class AuthSessionOptions
{
    public const string SessionCookie = "AUTHGATEWAY_SESSION";
    public const string RefreshCookie = "AUTHGATEWAY_REFRESH";
    public const string AccessCookie = "kb_session_access";

    public string SessionCookieName { get; set; } = SessionCookie;
    public string RefreshCookieName { get; set; } = RefreshCookie;
    public string AccessTokenCookieName { get; set; } = AccessCookie;
    public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan RefreshLifetime { get; set; } = TimeSpan.FromDays(7);
    public bool CookieSecure { get; set; } = true;
    public string CookieDomain { get; set; } = string.Empty;
    public string CookieSameSite { get; set; } = "Lax";
    public string RefreshSameSite { get; set; } = "Strict";
}
