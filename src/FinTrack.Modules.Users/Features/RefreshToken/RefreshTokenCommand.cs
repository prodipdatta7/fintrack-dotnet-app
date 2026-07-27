using MediatR;
using FinTrack.BuildingBlocks;

namespace FinTrack.Modules.Users.Features.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<RefreshTokenResponse>>;

public sealed record RefreshTokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
