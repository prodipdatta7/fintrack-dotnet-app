namespace FinTrack.Contracts.Transactions;

public record TransactionCreatedEvent(
    string TransactionId,
    string UserId,
    string AccountId,
    string CategoryId,
    string Title,
    decimal Amount,
    string Type,
    DateTime CreatedAt);

public record TransactionUpdatedEvent(
    string TransactionId,
    string UserId,
    string AccountId,
    string CategoryId,
    string Title,
    decimal Amount,
    string Type,
    DateTime UpdatedAt);
