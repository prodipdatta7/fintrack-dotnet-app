using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Users.Features.ChangePassword;

public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : IRequest<Result<ChangePasswordResponse>>;

public sealed record ChangePasswordResponse(string Message);
