using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Users.Features.Login;

[ApiController]
[Route("api/users/auth")]
public sealed class LoginController : ControllerBase
{
    private readonly ISender _sender;

    public LoginController(ISender sender) => _sender = sender;

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);

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

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        var isSecure = Request.IsHttps;
        var deleteOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = SameSiteMode.Lax
        };

        Response.Cookies.Delete("access_token", deleteOptions);
        Response.Cookies.Delete("refresh_token", deleteOptions);

        return Ok(new { message = "Logged out successfully." });
    }
}
