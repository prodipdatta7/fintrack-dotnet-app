namespace FinTrack.Modules.Assistant.Dtos;

public sealed record ProposedTransferPayload(
    decimal Amount,
    string FromAccountId,
    string FromAccountName,
    string ToAccountId,
    string ToAccountName,
    DateTime Date,
    string? Note = null);
