using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FinTrack.BuildingBlocks.Auth;

/// <summary>
/// Builds auth cookie options for local (HTTP, SameSite=Lax) and cloud
/// (HTTPS; Lax when frontend and API share an origin via Hosting rewrite).
/// Set Cors:CrossSiteCookies=true only if the SPA and API are on different sites.
/// </summary>
public static class AuthCookieOptions
{
    public static CookieOptions AccessToken(HttpRequest request, IConfiguration configuration, DateTimeOffset expires)
        => Create(request, configuration, expires);

    public static CookieOptions RefreshToken(HttpRequest request, IConfiguration configuration, DateTimeOffset expires)
        => Create(request, configuration, expires);

    public static CookieOptions Delete(HttpRequest request, IConfiguration configuration)
        => Create(request, configuration, expires: null);

    private static CookieOptions Create(HttpRequest request, IConfiguration configuration, DateTimeOffset? expires)
    {
        var crossSite = configuration.GetValue("Cors:CrossSiteCookies", false);
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = crossSite || request.IsHttps,
            SameSite = crossSite ? SameSiteMode.None : SameSiteMode.Lax,
        };

        if (expires.HasValue)
        {
            options.Expires = expires.Value;
        }

        return options;
    }
}
