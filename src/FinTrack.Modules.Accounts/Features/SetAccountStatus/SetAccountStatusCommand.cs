using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Accounts.Features.SetAccountStatus;

public sealed record SetAccountStatusCommand(string Id, bool IsClosed) : IRequest<Result>;

public sealed record StatusRequest(bool IsClosed);
