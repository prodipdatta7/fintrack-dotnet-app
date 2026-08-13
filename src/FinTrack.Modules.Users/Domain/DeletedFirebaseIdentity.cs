using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinTrack.Modules.Users.Domain;

/// <summary>
/// Tombstone for a Firebase uid whose FinTrack account was deleted. Prevents lazy re-provision
/// while an unexpired ID token is still cryptographically valid.
/// </summary>
public sealed class DeletedFirebaseIdentity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string FirebaseUid { get; set; } = string.Empty;

    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}
