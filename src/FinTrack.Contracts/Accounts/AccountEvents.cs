namespace FinTrack.Contracts.Accounts;

public record AccountCreatedEvent(
    string AccountId,
    string UserId,
    string Name,
    string Type,
    DateTime CreatedAt);

public record AccountClosedEvent(
    string AccountId,
    string UserId,
    DateTime ClosedAt);
