using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.GetSavingsPlansStatus;

public sealed record GetSavingsPlansStatusQuery(
    string? PlanId = null,
    string? PlanName = null) : IRequest<Result<SavingsPlansStatusResult>>;
