namespace FinTrack.BuildingBlocks.Auth;

public interface ICurrentUser
{
    string UserId { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
}
