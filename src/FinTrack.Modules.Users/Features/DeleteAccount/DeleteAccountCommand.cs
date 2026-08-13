using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Users.Features.DeleteAccount;

public sealed record DeleteAccountCommand(
    string ConfirmPassword) : IRequest<Result<DeleteAccountResponse>>;

public sealed record DeleteAccountResponse(string Message);
