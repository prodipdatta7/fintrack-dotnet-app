namespace FinTrack.Modules.Transactions.Features.GetTransactionEvents;

public sealed record TransactionEventDto(
    string Id,
    string TransactionId,
    string EventType,
    DateTime OccurredOnUtc,
    string Summary,
    string DataJson
);
