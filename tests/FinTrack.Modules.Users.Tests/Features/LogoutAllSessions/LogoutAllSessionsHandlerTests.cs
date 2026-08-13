using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Users.Domain;
using FinTrack.Modules.Users.Features.LogoutAllSessions;
using FinTrack.Modules.Users.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FinTrack.Modules.Users.Tests.Features.LogoutAllSessions;

public class LogoutAllSessionsHandlerTests
{
    private const string UserId = "user-123";
    private const string FirebaseUid = "firebase-uid-123";

    [Fact]
    public async Task Handle_WhenUserLinked_RevokesFirebaseTokensAndPersistsWatermark()
    {
        // Arrange
        var user = new User { Id = UserId, FirebaseUid = FirebaseUid };
        var users = SetupUsers(user);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(UserId);

        var firebaseAuth = Substitute.For<IFirebaseAuthService>();
        var logger = Substitute.For<ILogger<LogoutAllSessionsHandler>>();

        var handler = new LogoutAllSessionsHandler(
            Substitute.For<IMongoDatabase>().WithUsers(users), currentUser, firebaseAuth, logger);

        // Act
        var result = await handler.Handle(new LogoutAllSessionsCommand(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await firebaseAuth.Received(1).RevokeRefreshTokensAsync(FirebaseUid, Arg.Any<CancellationToken>());
        await users.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<User>>(),
            Arg.Any<UpdateDefinition<User>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserNotLinkedToFirebase_ReturnsFailureAndDoesNotRevoke()
    {
        // Arrange
        var user = new User { Id = UserId, FirebaseUid = "" };
        var users = SetupUsers(user);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(UserId);

        var firebaseAuth = Substitute.For<IFirebaseAuthService>();
        var logger = Substitute.For<ILogger<LogoutAllSessionsHandler>>();

        var handler = new LogoutAllSessionsHandler(
            Substitute.For<IMongoDatabase>().WithUsers(users), currentUser, firebaseAuth, logger);

        // Act
        var result = await handler.Handle(new LogoutAllSessionsCommand(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        await firebaseAuth.DidNotReceiveWithAnyArgs().RevokeRefreshTokensAsync(Arg.Any<string>(), default);
        await users.DidNotReceiveWithAnyArgs().UpdateOneAsync(
            Arg.Any<FilterDefinition<User>>(),
            Arg.Any<UpdateDefinition<User>>(),
            Arg.Any<UpdateOptions>(),
            default);
    }

    [Fact]
    public async Task Handle_WhenRevocationThrows_ReturnsFailure()
    {
        // Arrange
        var user = new User { Id = UserId, FirebaseUid = FirebaseUid };
        var users = SetupUsers(user);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(UserId);

        var firebaseAuth = Substitute.For<IFirebaseAuthService>();
        firebaseAuth.RevokeRefreshTokensAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("no credentials"));

        var logger = Substitute.For<ILogger<LogoutAllSessionsHandler>>();

        var handler = new LogoutAllSessionsHandler(
            Substitute.For<IMongoDatabase>().WithUsers(users), currentUser, firebaseAuth, logger);

        // Act
        var result = await handler.Handle(new LogoutAllSessionsCommand(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        await users.DidNotReceiveWithAnyArgs().UpdateOneAsync(
            Arg.Any<FilterDefinition<User>>(),
            Arg.Any<UpdateDefinition<User>>(),
            Arg.Any<UpdateOptions>(),
            default);
    }

    private static IMongoCollection<User> SetupUsers(User? found)
    {
        var cursor = MongoTestHelpers.CursorWith<User>(found);
        var users = Substitute.For<IMongoCollection<User>>();
        users.FindAsync<User>(Arg.Any<FilterDefinition<User>>(), Arg.Any<FindOptions<User, User>>(), Arg.Any<CancellationToken>())
            .Returns(cursor);
        return users;
    }
}

internal static class DatabaseExtensions
{
    public static IMongoDatabase WithUsers(this IMongoDatabase database, IMongoCollection<User> users)
    {
        database.GetCollection<User>("users").Returns(users);
        return database;
    }
}
