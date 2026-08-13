using FinTrack.BuildingBlocks;
using FinTrack.Modules.Budgets.Features.GetPlans;
using MediatR;

namespace FinTrack.Modules.Budgets.Features.DepositToPlan;

public sealed record DepositToPlanCommand(string Id, decimal Amount) : IRequest<Result<PlanDto>>;

public sealed record DepositRequest(decimal Amount);
