using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Budgets.Features.GetPlans;

public sealed record GetPlansQuery : IRequest<Result<List<PlanDto>>>;

public sealed record PlanDto(
    string Id,
    string Title,
    decimal TargetAmount,
    decimal CurrentAmount,
    string Color,
    DateTime Deadline);
