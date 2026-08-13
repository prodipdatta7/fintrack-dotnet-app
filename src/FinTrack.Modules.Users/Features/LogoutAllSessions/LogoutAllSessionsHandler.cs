using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Users.Domain;
using FinTrack.Modules.Users.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FinTrack.Modules.Users.Features.LogoutAllSessions;

internal sealed class LogoutAllSessionsHandler : IRequestHandler<LogoutAllSessionsCommand, Result<LogoutAllSessionsResponse>>
{
    private readonly IMongoCollection<User> _users;
    private readonly ICurrentUser _currentUser;
    private readonly IFirebaseAuthService _firebaseAuthService;
    private readonly ILogger<LogoutAllSessionsHandler> _logger;

    public LogoutAllSessionsHandler(
        IMongoDatabase database,
        ICurrentUser currentUser,
        IFirebaseAuthService firebaseAuthService,
        ILogger<LogoutAllSessionsHandler> logger)
    {
        _users = database.GetCollection<User>("users");
        _currentUser = currentUser;
        _firebaseAuthService = firebaseAuthService;
        _logger = logger;
    }

    public async Task<Result<LogoutAllSessionsResponse>> Handle(
        LogoutAllSessionsCommand request, CancellationToken cancellationToken)
    {
        var user = await _users
            .Find(u => u.Id == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result<LogoutAllSessionsResponse>.Failure("User not found.");

        if (string.IsNullOrWhiteSpace(user.FirebaseUid))
            return Result<LogoutAllSessionsResponse>.Failure("Account is not linked to Firebase.");

        var revokedAt = DateTime.UtcNow;

        try
        {
            await _firebaseAuthService.RevokeRefreshTokensAsync(user.FirebaseUid, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke Firebase sessions for user {UserId}", user.Id);
            return Result<LogoutAllSessionsResponse>.Failure(
                "Failed to revoke sessions. Firebase Admin credentials are not configured.");
        }

        // Persist watermark so JwtBearer rejects ID tokens whose auth_time is before revocation.
        await _users.UpdateOneAsync(
            u => u.Id == user.Id,
            Builders<User>.Update.Set(u => u.FirebaseTokensValidAfterUtc, revokedAt),
            cancellationToken: cancellationToken);

        return Result<LogoutAllSessionsResponse>.Success(
            new LogoutAllSessionsResponse("Logged out from all sessions successfully."));
    }
}
