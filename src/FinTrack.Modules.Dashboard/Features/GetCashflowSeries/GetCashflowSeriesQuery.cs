using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Dashboard.Features.GetCashflowSeries;

public sealed record GetCashflowSeriesQuery(
    string Timeframe = "30D",
    DateTime? From = null,
    DateTime? To = null,
    string? AccountId = null) : IRequest<Result<IReadOnlyList<CashflowPointDto>>>;

public sealed record CashflowPointDto(string Label, decimal Income, decimal Expense);
