using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Budgets.Features.DeletePlan;

public sealed record DeletePlanCommand(string Id) : IRequest<Result>;
