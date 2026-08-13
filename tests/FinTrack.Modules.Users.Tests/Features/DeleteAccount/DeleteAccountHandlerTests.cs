using FinTrack.BuildingBlocks.Auth;
using FinTrack.BuildingBlocks.Storage;
using FinTrack.Modules.Users.Domain;
using FinTrack.Modules.Users.Features.DeleteAccount;
using FinTrack.Modules.Users.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FinTrack.Modules.Users.Tests.Features.DeleteAccount;

public class DeleteAccountHandlerTests
{
    private const string UserId = "user-123";
    private const string FirebaseUid = "firebase-uid-123";

    [Fact]
    public async Task Handle_WhenUserFound_DeletesFirebaseAvatarSettingsAndUser()
    {
        // Arrange
        var user = new User
        {
            Id = UserId,
            FirebaseUid = FirebaseUid,
            AvatarUrl = "uploads/avatars/me.png"
        };
        var (users, settings, deleted, database) = Setup(user);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(UserId);

        var fileStorage = Substitute.For<IFileStorageService>();
        var firebaseAuth = Substitute.For<IFirebaseAuthService>();
        var logger = Substitute.For<ILogger<DeleteAccountHandler>>();

        var handler = new DeleteAccountHandler(
            database, currentUser, fileStorage, firebaseAuth, logger);

        // Act
        var result = await handler.Handle(new DeleteAccountCommand(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await deleted.Received(1).ReplaceOneAsync(
            Arg.Any<FilterDefinition<DeletedFirebaseIdentity>>(),
            Arg.Is<DeletedFirebaseIdentity>(d => d.FirebaseUid == FirebaseUid),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>());
        await firebaseAuth.Received(1).RevokeRefreshTokensAsync(FirebaseUid, Arg.Any<CancellationToken>());
        await firebaseAuth.Received(1).DeleteUserAsync(FirebaseUid, Arg.Any<CancellationToken>());
        await fileStorage.Received(1).DeleteFileAsync("uploads/avatars/me.png", Arg.Any<CancellationToken>());
        await users.Received(1).DeleteOneAsync(Arg.Any<FilterDefinition<User>>(), Arg.Any<CancellationToken>());
        await settings.Received(1).DeleteManyAsync(
            Arg.Any<FilterDefinition<UserSettings>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenFirebaseDeleteFails_ReturnsFailureAndKeepsMongoUser()
    {
        // Arrange
        var user = new User { Id = UserId, FirebaseUid = FirebaseUid };
        var (users, settings, _, database) = Setup(user);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(UserId);

        var fileStorage = Substitute.For<IFileStorageService>();
        var firebaseAuth = Substitute.For<IFirebaseAuthService>();
        firebaseAuth.RevokeRefreshTokensAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("no credentials"));
        var logger = Substitute.For<ILogger<DeleteAccountHandler>>();

        var handler = new DeleteAccountHandler(
            database, currentUser, fileStorage, firebaseAuth, logger);

        // Act
        var result = await handler.Handle(new DeleteAccountCommand(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        await users.DidNotReceiveWithAnyArgs().DeleteOneAsync(Arg.Any<FilterDefinition<User>>(), default);
        await settings.DidNotReceiveWithAnyArgs().DeleteManyAsync(Arg.Any<FilterDefinition<UserSettings>>(), default);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailureAndDeletesNothing()
    {
        // Arrange
        var (users, settings, deleted, database) = Setup(null);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(UserId);

        var fileStorage = Substitute.For<IFileStorageService>();
        var firebaseAuth = Substitute.For<IFirebaseAuthService>();
        var logger = Substitute.For<ILogger<DeleteAccountHandler>>();

        var handler = new DeleteAccountHandler(
            database, currentUser, fileStorage, firebaseAuth, logger);

        // Act
        var result = await handler.Handle(new DeleteAccountCommand(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        await fileStorage.DidNotReceiveWithAnyArgs().DeleteFileAsync(Arg.Any<string>(), default);
        await firebaseAuth.DidNotReceiveWithAnyArgs().DeleteUserAsync(Arg.Any<string>(), default);
        await deleted.DidNotReceiveWithAnyArgs().ReplaceOneAsync(
            Arg.Any<FilterDefinition<DeletedFirebaseIdentity>>(),
            Arg.Any<DeletedFirebaseIdentity>(),
            Arg.Any<ReplaceOptions>(),
            default);
        await users.DidNotReceiveWithAnyArgs().DeleteOneAsync(Arg.Any<FilterDefinition<User>>(), default);
        await settings.DidNotReceiveWithAnyArgs().DeleteManyAsync(Arg.Any<FilterDefinition<UserSettings>>(), default);
    }

    private static (
        IMongoCollection<User> Users,
        IMongoCollection<UserSettings> Settings,
        IMongoCollection<DeletedFirebaseIdentity> Deleted,
        IMongoDatabase Database)
        Setup(User? found)
    {
        var cursor = MongoTestHelpers.CursorWith<User>(found);
        var users = Substitute.For<IMongoCollection<User>>();
        users.FindAsync<User>(Arg.Any<FilterDefinition<User>>(), Arg.Any<FindOptions<User, User>>(), Arg.Any<CancellationToken>())
            .Returns(cursor);

        var settings = Substitute.For<IMongoCollection<UserSettings>>();
        var deleted = Substitute.For<IMongoCollection<DeletedFirebaseIdentity>>();

        var database = Substitute.For<IMongoDatabase>();
        database.GetCollection<User>("users").Returns(users);
        database.GetCollection<UserSettings>("user_settings").Returns(settings);
        database.GetCollection<DeletedFirebaseIdentity>("deleted_firebase_identities").Returns(deleted);

        return (users, settings, deleted, database);
    }
}
