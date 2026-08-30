namespace FinTrack.Modules.Assistant.Features.ProposeCreateSavingsPlan;

public sealed record ProposedCreateSavingsPlanPayload(
    string Title,
    decimal TargetAmount,
    decimal InitialAmount,
    DateTime Deadline,
    string Color);
