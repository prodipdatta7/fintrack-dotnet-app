using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Accounts.Features.CreateAccount;

public sealed record CreateAccountCommand(
    string Name,
    string AccountType,
    decimal Balance,
    string Currency,
    string Icon,
    string Provider,
    string Color) : IRequest<Result<string>>;
