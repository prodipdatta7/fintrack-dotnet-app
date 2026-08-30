namespace FinTrack.Modules.Assistant.Features.ProposeCreateCategory;

public sealed record ProposedCreateCategoryPayload(
    string Name,
    string Type,
    string Icon,
    string Color,
    decimal BudgetLimit,
    bool AlreadyExists);
