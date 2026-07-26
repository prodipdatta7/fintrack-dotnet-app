namespace FinTrack.BuildingBlocks;

/// <summary>
/// Provides the current user's identity to all modules.
/// Registered in Api via HttpContext; in Host, a system identity is used.
/// </summary>
public interface ICurrentUser
{
    string? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
