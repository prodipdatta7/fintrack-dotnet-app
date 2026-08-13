using FinTrack.BuildingBlocks.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FinTrack.Modules.Users.Features.RefreshToken;

[ApiController]
[Route("api")]
public sealed class RefreshTokenController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IConfiguration _configuration;

    public RefreshTokenController(ISender sender, IConfiguration configuration)
    {
        _sender = sender;
        _configuration = configuration;
    }

    [AllowAnonymous]
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

        Response.Cookies.Append(
            "access_token",
            result.Value!.AccessToken,
            AuthCookieOptions.AccessToken(Request, _configuration, result.Value!.ExpiresAt));

        Response.Cookies.Append(
            "refresh_token",
            result.Value!.RefreshToken,
            AuthCookieOptions.RefreshToken(Request, _configuration, DateTime.UtcNow.AddDays(7)));

        return Ok(result.Value);
    }
}
