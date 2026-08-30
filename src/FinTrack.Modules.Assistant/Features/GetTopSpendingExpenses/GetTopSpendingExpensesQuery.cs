using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.GetTopSpendingExpenses;

public sealed record GetTopSpendingExpensesQuery(
    string? Period = "this_month",
    int Limit = 5) : IRequest<Result<TopSpendingExpensesResult>>;
