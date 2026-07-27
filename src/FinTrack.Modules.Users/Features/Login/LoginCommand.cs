using MediatR;
using FinTrack.BuildingBlocks;

namespace FinTrack.Modules.Users.Features.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>;

public sealed record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
