using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Accounts.Features.UpdateAccountBalance;

public sealed record UpdateAccountBalanceCommand(string Id, decimal Balance) : IRequest<Result>;

public sealed record BalanceRequest(decimal Balance);
