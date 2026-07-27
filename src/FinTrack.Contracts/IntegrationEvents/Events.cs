namespace FinTrack.Contracts.IntegrationEvents;

public sealed record UserRegistered(string UserId, string Email, DateTime RegisteredAt);

public sealed record TransactionCreated(
    string TransactionId,
    string UserId,
    string AccountId,
    string CategoryId,
    decimal Amount,
    string Type,
    DateTime Date);

public sealed record TransactionUpdated(
    string TransactionId,
    string UserId,
    string AccountId,
    string CategoryId,
    decimal Amount,
    decimal PreviousAmount,
    string Type,
    DateTime Date);

public sealed record CategoryCreated(string CategoryId, string UserId, string Name, string Type);
