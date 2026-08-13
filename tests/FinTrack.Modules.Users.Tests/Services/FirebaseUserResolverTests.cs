using System.Net;
using FinTrack.Contracts.IntegrationEvents;
using FinTrack.Modules.Users.Domain;
using FinTrack.Modules.Users.Services;
using FluentAssertions;
using MassTransit;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FinTrack.Modules.Users.Tests.Services;

public class FirebaseUserResolverTests
{
    private const string FirebaseUid = "firebase-uid-123";
    private const string Email = "John.Doe@Example.COM";
    private const string Name = "John Doe";

    [Fact]
    public async Task ResolveUserIdAsync_WhenUserAlreadyLinked_ReturnsMongoIdWithoutInserting()
    {
        // Arrange
        var existingUser = new User { Id = "mongo-id-1", FirebaseUid = FirebaseUid, Email = "john.doe@example.com" };
        var (users, _, database) = SetupCollection(existingUser);
        var publishEndpoint = Substitute.For<IPublishEndpoint>();

        var resolver = new FirebaseUserResolver(database, publishEndpoint);

        // Act
        var id = await resolver.ResolveUserIdAsync(
            FirebaseUid, Email, Name, authTime: null, CancellationToken.None);

        // Assert
        id.Should().Be("mongo-id-1");
        await users.DidNotReceiveWithAnyArgs().InsertOneAsync(Arg.Any<User>(), null, default);
        await publishEndpoint.DidNotReceiveWithAnyArgs().Publish<UserRegistered>(Arg.Any<UserRegistered>(), default);
    }

    [Fact]
    public async Task ResolveUserIdAsync_WhenTokenRevoked_ThrowsSecurityTokenException()
    {
        // Arrange
        var revokedAt = DateTime.UtcNow;
        var existingUser = new User
        {
            Id = "mongo-id-1",
            FirebaseUid = FirebaseUid,
            FirebaseTokensValidAfterUtc = revokedAt
        };
        var (_, _, database) = SetupCollection(existingUser);
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var resolver = new FirebaseUserResolver(database, publishEndpoint);

        var authTime = new DateTimeOffset(revokedAt.AddMinutes(-5));

        // Act
        var act = () => resolver.ResolveUserIdAsync(
            FirebaseUid, Email, Name, authTime, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SecurityTokenException>()
            .WithMessage("*revoked*");
    }

    [Fact]
    public async Task ResolveUserIdAsync_WhenIdentityTombstoned_ThrowsAndDoesNotProvision()
    {
        // Arrange
        var (users, _, database) = SetupCollection(null, tombstoned: true);
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var resolver = new FirebaseUserResolver(database, publishEndpoint);

        // Act
        var act = () => resolver.ResolveUserIdAsync(
            FirebaseUid, Email, Name, authTime: null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SecurityTokenException>()
            .WithMessage("*deleted*");
        await users.DidNotReceiveWithAnyArgs().InsertOneAsync(Arg.Any<User>(), null, default);
    }

    [Fact]
    public async Task ResolveUserIdAsync_WhenNoExistingUser_CreatesUserAndPublishesRegistered()
    {
        // Arrange
        var (users, _, database) = SetupCollection(null);
        User? inserted = null;
        users.InsertOneAsync(
                Arg.Do<User>(u => inserted = u),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var resolver = new FirebaseUserResolver(database, publishEndpoint);

        // Act
        var id = await resolver.ResolveUserIdAsync(
            FirebaseUid, Email, Name, authTime: null, CancellationToken.None);

        // Assert
        id.Should().Be(inserted!.Id);
        inserted.FirebaseUid.Should().Be(FirebaseUid);
        inserted.Email.Should().Be("john.doe@example.com");
        inserted.FirstName.Should().Be("John");
        inserted.LastName.Should().Be("Doe");
        inserted.UserId.Should().Be(inserted.Id);

        await publishEndpoint.Received(1).Publish(
            Arg.Is<UserRegistered>(e => e.UserId == inserted.Id && e.Email == inserted.Email),
            CancellationToken.None);
    }

    [Fact]
    public async Task ResolveUserIdAsync_WhenDuplicateKeyRace_RefindsAndReturnsExisting()
    {
        // Arrange
        var callCount = 0;
        var existingUser = new User { Id = "mongo-id-winner", FirebaseUid = FirebaseUid };
        var users = Substitute.For<IMongoCollection<User>>();
        users.FindAsync<User>(Arg.Any<FilterDefinition<User>>(), Arg.Any<FindOptions<User, User>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                return callCount == 1
                    ? MongoTestHelpers.CursorWith<User>(null)
                    : MongoTestHelpers.CursorWith<User>(existingUser);
            });
        users.InsertOneAsync(Arg.Any<User>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .Throws(CreateDuplicateKeyException());
        users.Indexes.CreateOneAsync(
                Arg.Any<CreateIndexModel<User>>(),
                Arg.Any<CreateOneIndexOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("uq_users_firebase_uid"));

        var deletedCursor = MongoTestHelpers.CursorWith<DeletedFirebaseIdentity>(null);
        var deleted = Substitute.For<IMongoCollection<DeletedFirebaseIdentity>>();
        deleted.FindAsync(
                Arg.Any<FilterDefinition<DeletedFirebaseIdentity>>(),
                Arg.Any<FindOptions<DeletedFirebaseIdentity, DeletedFirebaseIdentity>>(),
                Arg.Any<CancellationToken>())
            .Returns(deletedCursor);
        deleted.Indexes.CreateOneAsync(
                Arg.Any<CreateIndexModel<DeletedFirebaseIdentity>>(),
                Arg.Any<CreateOneIndexOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("uq_deleted_firebase_uid"));

        var database = Substitute.For<IMongoDatabase>();
        database.GetCollection<User>("users").Returns(users);
        database.GetCollection<DeletedFirebaseIdentity>("deleted_firebase_identities").Returns(deleted);

        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var resolver = new FirebaseUserResolver(database, publishEndpoint);

        // Act
        var id = await resolver.ResolveUserIdAsync(
            FirebaseUid, Email, Name, authTime: null, CancellationToken.None);

        // Assert
        id.Should().Be("mongo-id-winner");
        await publishEndpoint.DidNotReceiveWithAnyArgs().Publish<UserRegistered>(Arg.Any<UserRegistered>(), default);
    }

    [Fact]
    public async Task ResolveUserIdAsync_WhenNonDuplicateInsertFails_Rethrows()
    {
        // Arrange
        var (users, _, database) = SetupCollection(null);
        users.InsertOneAsync(Arg.Any<User>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("network blip"));

        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var resolver = new FirebaseUserResolver(database, publishEndpoint);

        // Act
        var act = () => resolver.ResolveUserIdAsync(
            FirebaseUid, Email, Name, authTime: null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("network blip");
        await publishEndpoint.DidNotReceiveWithAnyArgs().Publish<UserRegistered>(Arg.Any<UserRegistered>(), default);
    }

    [Fact]
    public async Task ResolveUserIdAsync_NeverLinksByEmailAlone_WhenExistingUserHasSameEmail()
    {
        // Arrange
        var (users, _, database) = SetupCollection(null);
        User? inserted = null;
        users.InsertOneAsync(
                Arg.Do<User>(u => inserted = u),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var resolver = new FirebaseUserResolver(database, publishEndpoint);

        // Act
        var id = await resolver.ResolveUserIdAsync(
            FirebaseUid, Email, Name, authTime: null, CancellationToken.None);

        // Assert
        id.Should().NotBe("legacy-id");
        inserted.Should().NotBeNull();
        inserted!.FirebaseUid.Should().Be(FirebaseUid);
    }

    [Fact]
    public async Task ResolveUserIdAsync_WhenNoName_LeavesNamesEmpty()
    {
        // Arrange
        var (users, _, database) = SetupCollection(null);
        User? inserted = null;
        users.InsertOneAsync(
                Arg.Do<User>(u => inserted = u),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var resolver = new FirebaseUserResolver(database, publishEndpoint);

        // Act
        await resolver.ResolveUserIdAsync(
            FirebaseUid, Email, null, authTime: null, CancellationToken.None);

        // Assert
        inserted!.FirstName.Should().BeEmpty();
        inserted.LastName.Should().BeEmpty();
    }

    private static (
        IMongoCollection<User> Users,
        IMongoCollection<DeletedFirebaseIdentity> Deleted,
        IMongoDatabase Database)
        SetupCollection(User? found, bool tombstoned = false)
    {
        var userCursor = MongoTestHelpers.CursorWith<User>(found);
        var users = Substitute.For<IMongoCollection<User>>();
        users.FindAsync<User>(Arg.Any<FilterDefinition<User>>(), Arg.Any<FindOptions<User, User>>(), Arg.Any<CancellationToken>())
            .Returns(userCursor);
        users.Indexes.CreateOneAsync(
                Arg.Any<CreateIndexModel<User>>(),
                Arg.Any<CreateOneIndexOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("uq_users_firebase_uid"));

        var tombstone = tombstoned
            ? new DeletedFirebaseIdentity { FirebaseUid = FirebaseUid }
            : null;
        var deletedCursor = MongoTestHelpers.CursorWith(tombstone);
        var deleted = Substitute.For<IMongoCollection<DeletedFirebaseIdentity>>();
        deleted.FindAsync(
                Arg.Any<FilterDefinition<DeletedFirebaseIdentity>>(),
                Arg.Any<FindOptions<DeletedFirebaseIdentity, DeletedFirebaseIdentity>>(),
                Arg.Any<CancellationToken>())
            .Returns(deletedCursor);
        deleted.Indexes.CreateOneAsync(
                Arg.Any<CreateIndexModel<DeletedFirebaseIdentity>>(),
                Arg.Any<CreateOneIndexOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("uq_deleted_firebase_uid"));

        var database = Substitute.For<IMongoDatabase>();
        database.GetCollection<User>("users").Returns(users);
        database.GetCollection<DeletedFirebaseIdentity>("deleted_firebase_identities").Returns(deleted);

        return (users, deleted, database);
    }

    private static MongoWriteException CreateDuplicateKeyException()
    {
        var writeError = (WriteError)Activator.CreateInstance(
            typeof(WriteError),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            args: [ServerErrorCategory.DuplicateKey, 11000, "E11000 duplicate key", new BsonDocument()],
            culture: null)!;

        var connectionId = new ConnectionId(
            new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Loopback, 27017)));
        return new MongoWriteException(connectionId, writeError, null, null);
    }
}
