namespace FinTrack.Modules.Assistant.Features.GetSavingsPlansStatus;

public sealed record SavingsPlanStatusItemDto(
    string PlanId,
    string Title,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal RemainingAmount,
    decimal ProgressPercentage,
    string Color,
    DateTime Deadline,
    int DaysRemaining,
    bool IsCompleted);

public sealed record SavingsPlansStatusResult(
    decimal TotalTargetAmount,
    decimal TotalCurrentAmount,
    decimal TotalRemainingAmount,
    decimal OverallProgressPercentage,
    int PlanCount,
    IReadOnlyList<SavingsPlanStatusItemDto> Plans);
