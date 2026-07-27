using MediatR;
using FinTrack.BuildingBlocks;

namespace FinTrack.Modules.Users.Features.GetMe;

public sealed record GetMeQuery : IRequest<Result<GetMeResponse>>;

public sealed record GetMeResponse(
    string UserId,
    string Email,
    string FirstName,
    string LastName);
