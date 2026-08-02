using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Users.Features.LogoutAllSessions;

public sealed record LogoutAllSessionsCommand : IRequest<Result<LogoutAllSessionsResponse>>;

public sealed record LogoutAllSessionsResponse(string Message);
