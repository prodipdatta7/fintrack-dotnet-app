using MediatR;

namespace FinTrack.Modules.Users.Features.Login;

public record LoginCommand(
    string Email,
    string Password) : IRequest<LoginResult>;

public record LoginResult(
    string UserId,
    string Email,
    string AccessToken,
    string RefreshToken);
