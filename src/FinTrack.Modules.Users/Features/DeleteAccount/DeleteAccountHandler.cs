using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.BuildingBlocks.Storage;
using FinTrack.Modules.Users.Domain;
using FinTrack.Modules.Users.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FinTrack.Modules.Users.Features.DeleteAccount;

internal sealed class DeleteAccountHandler : IRequestHandler<DeleteAccountCommand, Result<DeleteAccountResponse>>
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<UserSettings> _userSettings;
    private readonly IMongoCollection<DeletedFirebaseIdentity> _deletedIdentities;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFirebaseAuthService _firebaseAuthService;
    private readonly ILogger<DeleteAccountHandler> _logger;

    public DeleteAccountHandler(
        IMongoDatabase database,
        ICurrentUser currentUser,
        IFileStorageService fileStorageService,
        IFirebaseAuthService firebaseAuthService,
        ILogger<DeleteAccountHandler> logger)
    {
        _users = database.GetCollection<User>("users");
        _userSettings = database.GetCollection<UserSettings>("user_settings");
        _deletedIdentities = database.GetCollection<DeletedFirebaseIdentity>("deleted_firebase_identities");
        _currentUser = currentUser;
        _fileStorageService = fileStorageService;
        _firebaseAuthService = firebaseAuthService;
        _logger = logger;
    }

    public async Task<Result<DeleteAccountResponse>> Handle(
        DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _users
            .Find(u => u.Id == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result<DeleteAccountResponse>.Failure("User not found.");

        if (!string.IsNullOrWhiteSpace(user.FirebaseUid))
        {
            // Tombstone first so an in-flight ID token cannot re-provision after Mongo delete.
            await _deletedIdentities.ReplaceOneAsync(
                d => d.FirebaseUid == user.FirebaseUid,
                new DeletedFirebaseIdentity { FirebaseUid = user.FirebaseUid },
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);

            try
            {
                await _firebaseAuthService.RevokeRefreshTokensAsync(user.FirebaseUid, cancellationToken);
                await _firebaseAuthService.DeleteUserAsync(user.FirebaseUid, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete Firebase user {FirebaseUid} for {UserId}",
                    user.FirebaseUid, user.Id);
                return Result<DeleteAccountResponse>.Failure(
                    "Failed to delete Firebase account. Firebase Admin credentials may not be configured.");
            }
        }

        if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            await _fileStorageService.DeleteFileAsync(user.AvatarUrl, cancellationToken);
        }

        await _users.DeleteOneAsync(u => u.Id == user.Id, cancellationToken);
        await _userSettings.DeleteManyAsync(s => s.UserId == user.Id, cancellationToken);

        return Result<DeleteAccountResponse>.Success(
            new DeleteAccountResponse("Account deleted successfully."));
    }
}
