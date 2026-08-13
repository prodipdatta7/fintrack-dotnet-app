using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Accounts.Features.UpdateAccount;

// Balance is intentionally absent — it changes only via PATCH /api/accounts/{id}/balance,
// so a stale edit form can never clobber a concurrent inline balance adjustment.
public sealed record UpdateAccountCommand(
    string Id,
    string Name,
    string AccountType,
    string Currency,
    string Icon,
    string Provider,
    string Color) : IRequest<Result>;
