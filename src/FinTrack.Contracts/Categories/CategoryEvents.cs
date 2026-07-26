namespace FinTrack.Contracts.Categories;

public record CategoryCreatedEvent(
    string CategoryId,
    string UserId,
    string Name,
    string Type,
    DateTime CreatedAt);
