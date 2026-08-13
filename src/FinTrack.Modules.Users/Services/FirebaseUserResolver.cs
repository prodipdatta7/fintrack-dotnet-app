using FinTrack.Contracts.IntegrationEvents;
using FinTrack.Modules.Users.Domain;
using MassTransit;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Threading;

namespace FinTrack.Modules.Users.Services;

/// <summary>
/// Resolves a verified Firebase uid to the Mongo user id, lazily provisioning a user document
/// on first sign-in. Never links by email alone (protects against account-takeover during the
/// Firebase migration). Safe under concurrent first-sign-ins: the unique index on FirebaseUid
/// is the arbiter of duplicate-key races.
/// </summary>
public sealed class FirebaseUserResolver : IFirebaseUserResolver
{
    private static int _indexCreated;

    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<DeletedFirebaseIdentity> _deletedIdentities;
    private readonly IPublishEndpoint _publishEndpoint;

    public FirebaseUserResolver(IMongoDatabase database, IPublishEndpoint publishEndpoint)
    {
        _users = database.GetCollection<User>("users");
        _deletedIdentities = database.GetCollection<DeletedFirebaseIdentity>("deleted_firebase_identities");
        _publishEndpoint = publishEndpoint;
    }

    public async Task<string> ResolveUserIdAsync(
        string firebaseUid,
        string? email,
        string? name,
        DateTimeOffset? authTime,
        CancellationToken cancellationToken)
    {
        await EnsureFirebaseUidIndexAsync(cancellationToken);

        if (await IsDeletedIdentityAsync(firebaseUid, cancellationToken))
        {
            throw new SecurityTokenException("Firebase identity has been deleted.");
        }

        var existing = await _users
            .Find(u => u.FirebaseUid == firebaseUid)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            EnsureTokenNotRevoked(existing, authTime);
            return existing.Id;
        }

        var (firstName, lastName) = SplitName(name);

        var user = new User
        {
            Email = (email ?? string.Empty).Trim().ToLowerInvariant(),
            FirebaseUid = firebaseUid,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = string.Empty,
            UserId = string.Empty,
            CreatedBy = "firebase"
        };
        user.UserId = user.Id;

        try
        {
            await _users.InsertOneAsync(user, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Concurrent first sign-in won the unique FirebaseUid race — adopt that document.
            var winner = await _users
                .Find(u => u.FirebaseUid == firebaseUid)
                .FirstOrDefaultAsync(cancellationToken);

            if (winner is not null)
            {
                EnsureTokenNotRevoked(winner, authTime);
                return winner.Id;
            }

            throw;
        }

        // Publish with CancellationToken.None so default seeding is never skipped even if the
        // inbound request is cancelled right after the insert.
        await _publishEndpoint.Publish(
            new UserRegistered(user.Id, user.Email, user.CreatedAt),
            CancellationToken.None);

        return user.Id;
    }

    private async Task<bool> IsDeletedIdentityAsync(string firebaseUid, CancellationToken cancellationToken)
    {
        var deleted = await _deletedIdentities
            .Find(d => d.FirebaseUid == firebaseUid)
            .FirstOrDefaultAsync(cancellationToken);

        return deleted is not null;
    }

    private static void EnsureTokenNotRevoked(User user, DateTimeOffset? authTime)
    {
        if (user.FirebaseTokensValidAfterUtc is not DateTime validAfter || authTime is null)
            return;

        // Reject tokens whose auth_time is strictly before the revocation watermark.
        if (authTime.Value.UtcDateTime < validAfter)
        {
            throw new SecurityTokenException("Firebase ID token has been revoked.");
        }
    }

    private async Task EnsureFirebaseUidIndexAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _indexCreated) == 1)
            return;

        var model = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.FirebaseUid),
            new CreateIndexOptions
            {
                Unique = true,
                Sparse = true,
                Name = "uq_users_firebase_uid"
            });

        await _users.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);

        var deletedModel = new CreateIndexModel<DeletedFirebaseIdentity>(
            Builders<DeletedFirebaseIdentity>.IndexKeys.Ascending(d => d.FirebaseUid),
            new CreateIndexOptions
            {
                Unique = true,
                Name = "uq_deleted_firebase_uid"
            });

        await _deletedIdentities.Indexes.CreateOneAsync(deletedModel, cancellationToken: cancellationToken);
        Interlocked.Exchange(ref _indexCreated, 1);
    }

    private static (string FirstName, string LastName) SplitName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (string.Empty, string.Empty);

        var parts = name.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1
            ? (parts[0], parts[1])
            : (parts[0], string.Empty);
    }
}
