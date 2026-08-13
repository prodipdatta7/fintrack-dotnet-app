using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Budgets.Features.UpdatePlan;

public sealed record UpdatePlanCommand(
    string Id,
    string Title,
    decimal TargetAmount,
    decimal CurrentAmount,
    string Color,
    DateTime Deadline) : IRequest<Result>;
