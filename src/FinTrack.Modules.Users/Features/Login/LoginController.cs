using FinTrack.BuildingBlocks.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FinTrack.Modules.Users.Features.Login;

[ApiController]
[Route("api")]
public sealed class LoginController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IConfiguration _configuration;

    public LoginController(ISender sender, IConfiguration configuration)
    {
        _sender = sender;
        _configuration = configuration;
    }

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

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        var deleteOptions = AuthCookieOptions.Delete(Request, _configuration);
        Response.Cookies.Delete("access_token", deleteOptions);
        Response.Cookies.Delete("refresh_token", deleteOptions);

        return Ok(new { message = "Logged out successfully." });
    }
}
