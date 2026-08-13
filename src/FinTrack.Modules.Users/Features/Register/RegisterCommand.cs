using MediatR;
using FinTrack.BuildingBlocks;

namespace FinTrack.Modules.Users.Features.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : IRequest<Result<RegisterResponse>>;

public sealed record RegisterResponse(string UserId, string Email);
