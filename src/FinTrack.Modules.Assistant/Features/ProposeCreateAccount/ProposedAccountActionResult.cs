namespace FinTrack.Modules.Assistant.Features.ProposeCreateAccount;

public sealed record ProposedCreateAccountPayload(
    string Name,
    string AccountType,
    decimal InitialBalance,
    string Currency,
    string Color,
    bool AlreadyExists);
