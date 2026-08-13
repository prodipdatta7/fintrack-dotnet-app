using FinTrack.BuildingBlocks;
using FinTrack.Modules.Accounts.Features.GetAccounts;
using MediatR;

namespace FinTrack.Modules.Accounts.Features.GetAccount;

public sealed record GetAccountQuery(string Id) : IRequest<Result<AccountDto>>;
