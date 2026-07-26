using MediatR;

namespace FinTrack.Modules.Users.Features.TokenRefresh;

public record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken) : IRequest<RefreshTokenResult>;

public record RefreshTokenResult(
    string UserId,
    string Email,
    string AccessToken,
    string RefreshToken);
