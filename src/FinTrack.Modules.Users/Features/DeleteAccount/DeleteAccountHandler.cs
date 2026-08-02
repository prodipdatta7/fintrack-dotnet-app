using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.BuildingBlocks.Storage;
using FinTrack.Modules.Users.Domain;
using FinTrack.Modules.Users.Services;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Users.Features.DeleteAccount;

internal sealed class DeleteAccountHandler : IRequestHandler<DeleteAccountCommand, Result<DeleteAccountResponse>>
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<UserSettings> _userSettings;
    private readonly IMongoCollection<Domain.RefreshToken> _refreshTokens;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IFileStorageService _fileStorageService;

    public DeleteAccountHandler(
        IMongoDatabase database,
        ICurrentUser currentUser,
        IPasswordHasher passwordHasher,
        IFileStorageService fileStorageService)
    {
        _users = database.GetCollection<User>("users");
        _userSettings = database.GetCollection<UserSettings>("user_settings");
        _refreshTokens = database.GetCollection<Domain.RefreshToken>("refresh_tokens");
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _fileStorageService = fileStorageService;
    }

    public async Task<Result<DeleteAccountResponse>> Handle(
        DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _users
            .Find(u => u.Id == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result<DeleteAccountResponse>.Failure("User not found.");

        if (!_passwordHasher.Verify(request.ConfirmPassword, user.PasswordHash))
            return Result<DeleteAccountResponse>.Failure("Password confirmation failed. Incorrect password.");

        // Delete user's avatar file if present
        if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            await _fileStorageService.DeleteFileAsync(user.AvatarUrl, cancellationToken);
        }

        // Remove user, settings, and refresh tokens
        await _users.DeleteOneAsync(u => u.Id == user.Id, cancellationToken);
        await _userSettings.DeleteManyAsync(s => s.UserId == user.Id, cancellationToken);
        await _refreshTokens.DeleteManyAsync(rt => rt.UserId == user.Id, cancellationToken);

        return Result<DeleteAccountResponse>.Success(
            new DeleteAccountResponse("Account deleted successfully."));
    }
}
