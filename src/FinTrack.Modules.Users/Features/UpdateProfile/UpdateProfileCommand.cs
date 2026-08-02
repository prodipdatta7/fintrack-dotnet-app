using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Users.Features.UpdateProfile;

public sealed record UpdateProfileCommand(
    string Email,
    string FirstName,
    string LastName) : IRequest<Result<UpdateProfileResponse>>;

public sealed record UpdateProfileResponse(
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    string AvatarUrl);
