namespace FinTrack.Contracts.Users;

public record UserRegisteredEvent(
    string UserId,
    string Email,
    DateTime RegisteredAt);
