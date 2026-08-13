namespace FinTrack.Modules.Users.Services;

public interface IFirebaseUserResolver
{
    /// <summary>
    /// Resolves a verified Firebase uid to the Mongo user id, lazily provisioning a user
    /// document on first sign-in. Never links by email alone. Rejects revoked tokens
    /// (<paramref name="authTime"/> before <c>FirebaseTokensValidAfterUtc</c>) and deleted
    /// identities (tombstone).
    /// </summary>
    Task<string> ResolveUserIdAsync(
        string firebaseUid,
        string? email,
        string? name,
        DateTimeOffset? authTime,
        CancellationToken cancellationToken);
}
