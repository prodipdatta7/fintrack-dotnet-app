using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.GetPortfolioOrAccountBalance;

public sealed record GetPortfolioOrAccountBalanceQuery(
    string? AccountId = null,
    string? AccountName = null) : IRequest<Result<PortfolioOrAccountBalanceResult>>;
