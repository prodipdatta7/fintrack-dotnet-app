using MediatR;

namespace FinTrack.Modules.Users.Features.Register;

public record RegisterCommand(
    string Email,
    string Password) : IRequest<RegisterResult>;

public record RegisterResult(
    string UserId,
    string Email,
    string AccessToken,
    string RefreshToken);
