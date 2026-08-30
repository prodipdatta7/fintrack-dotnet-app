using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.ProposeCreateCategory;

public sealed record ProposeCreateCategoryCommand(
    string Name,
    string? Type = "Expense",
    string? Icon = "tag",
    string? Color = "#6366f1",
    decimal? BudgetLimit = 0) : IRequest<Result<ProposedActionDto<ProposedCreateCategoryPayload>>>;
