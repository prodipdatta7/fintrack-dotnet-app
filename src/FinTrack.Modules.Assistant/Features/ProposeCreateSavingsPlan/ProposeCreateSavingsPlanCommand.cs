using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.ProposeCreateSavingsPlan;

public sealed record ProposeCreateSavingsPlanCommand(
    string Name,
    decimal TargetAmount,
    DateTime? TargetDate = null,
    decimal? InitialAmount = 0,
    string? Color = "#6366f1") : IRequest<Result<ProposedActionDto<ProposedCreateSavingsPlanPayload>>>;
