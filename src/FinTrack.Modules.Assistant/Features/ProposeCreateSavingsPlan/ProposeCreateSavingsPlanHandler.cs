using System.Globalization;
using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.ProposeCreateSavingsPlan;

internal sealed class ProposeCreateSavingsPlanHandler
    : IRequestHandler<ProposeCreateSavingsPlanCommand, Result<ProposedActionDto<ProposedCreateSavingsPlanPayload>>>
{
    public Task<Result<ProposedActionDto<ProposedCreateSavingsPlanPayload>>> Handle(
        ProposeCreateSavingsPlanCommand request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        var targetAmount = request.TargetAmount;
        var initialAmount = request.InitialAmount ?? 0m;
        var color = string.IsNullOrWhiteSpace(request.Color) ? "#6366f1" : request.Color.Trim();
        var deadline = request.TargetDate.HasValue
            ? request.TargetDate.Value.ToUniversalTime()
            : DateTime.UtcNow.AddMonths(6);

        var formattedDate = deadline.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        var summary = $"Create new savings plan \"{name}\" with a target goal of ৳{targetAmount:N0} (starting at ৳{initialAmount:N0}) by {formattedDate}.";

        var payload = new ProposedCreateSavingsPlanPayload(
            Title: name,
            TargetAmount: targetAmount,
            InitialAmount: initialAmount,
            Deadline: deadline,
            Color: color);

        var proposedAction = ProposedActionDto<ProposedCreateSavingsPlanPayload>.Create(
            actionType: "CreateSavingsPlan",
            summary: summary,
            payload: payload);

        return Task.FromResult(Result<ProposedActionDto<ProposedCreateSavingsPlanPayload>>.Success(proposedAction));
    }
}
