using FinTrack.BuildingBlocks.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace FinTrack.Modules.Users.Domain;

public sealed class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Legacy local-password hash. Unused under Firebase Auth; kept only until pre-migration
    /// documents are retired.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Firebase Auth uid. Set on first authenticated call (lazy provision). Omitted from BSON
    /// when empty so the sparse unique index is not poisoned by legacy documents.
    /// </summary>
    [BsonIgnoreIfDefault]
    public string FirebaseUid { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp after which Firebase ID tokens for this user are rejected. Set when
    /// sessions are revoked (logout-all). Compared to the token's <c>auth_time</c> claim.
    /// </summary>
    [BsonIgnoreIfNull]
    public DateTime? FirebaseTokensValidAfterUtc { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;

    [BsonIgnore]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
