using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Users.Features.RefreshToken;

[ApiController]
[Route("api/users/auth")]
public sealed class RefreshTokenController : ControllerBase
{
    private readonly ISender _sender;

    public RefreshTokenController(ISender sender) => _sender = sender;

    [AllowAnonymous]
    [HttpPost("refresh")]
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenCommand? command,
        CancellationToken ct)
    {
        var tokenToUse = command?.RefreshToken;
        if (string.IsNullOrEmpty(tokenToUse) && Request.Cookies.TryGetValue("refresh_token", out var cookieRefreshToken))
        {
            tokenToUse = cookieRefreshToken;
        }

        if (string.IsNullOrEmpty(tokenToUse))
        {
            return BadRequest(new { error = "Refresh token is required." });
        }

        var result = await _sender.Send(new RefreshTokenCommand(tokenToUse), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        var isSecure = Request.IsHttps;
        var cookieOptionsAccess = new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = SameSiteMode.Lax,
            Expires = result.Value!.ExpiresAt
        };

        var cookieOptionsRefresh = new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("access_token", result.Value!.AccessToken, cookieOptionsAccess);
        Response.Cookies.Append("refresh_token", result.Value!.RefreshToken, cookieOptionsRefresh);

        return Ok(result.Value);
    }
}
