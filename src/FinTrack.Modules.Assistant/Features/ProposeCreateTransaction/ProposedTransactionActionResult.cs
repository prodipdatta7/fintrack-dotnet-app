namespace FinTrack.Modules.Assistant.Features.ProposeCreateTransaction;

public sealed record ProposedCreateTransactionPayload(
    decimal Amount,
    string Type,
    string CategoryId,
    string CategoryName,
    string AccountId,
    string AccountName,
    string Title,
    DateTime Date,
    string Note);
