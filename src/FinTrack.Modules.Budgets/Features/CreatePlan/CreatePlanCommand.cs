using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Budgets.Features.CreatePlan;

public sealed record CreatePlanCommand(
    string Title,
    decimal TargetAmount,
    decimal CurrentAmount,
    string Color,
    DateTime Deadline) : IRequest<Result<string>>;
