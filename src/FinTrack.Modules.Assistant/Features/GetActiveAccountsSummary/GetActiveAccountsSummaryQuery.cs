using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.GetActiveAccountsSummary;

public sealed record GetActiveAccountsSummaryQuery(
    bool IncludeClosed = false) : IRequest<Result<ActiveAccountsSummaryResult>>;
