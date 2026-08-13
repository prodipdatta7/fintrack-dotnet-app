namespace FinTrack.Modules.Users.Services;

public interface IFirebaseAuthService
{
    /// <summary>Revokes all refresh tokens issued to the user, invalidating existing ID sessions.</summary>
    Task RevokeRefreshTokensAsync(string firebaseUid, CancellationToken cancellationToken);

    /// <summary>Deletes the Firebase Authentication user. No-ops if the user is already gone.</summary>
    Task DeleteUserAsync(string firebaseUid, CancellationToken cancellationToken);
}
