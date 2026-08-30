using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.GetCategorySpendingVsBudget;

public sealed record GetCategorySpendingVsBudgetQuery(
    string Category,
    string? Period = "this_month") : IRequest<Result<CategorySpendingVsBudgetResult>>;
