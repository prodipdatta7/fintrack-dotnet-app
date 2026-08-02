using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.BuildingBlocks.Storage;
using FinTrack.Modules.Users.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Users.Features.UploadAvatar;

internal sealed class UploadAvatarHandler : IRequestHandler<UploadAvatarCommand, Result<UploadAvatarResponse>>
{
    private readonly IMongoCollection<User> _users;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorageService _fileStorageService;

    public UploadAvatarHandler(
        IMongoDatabase database,
        ICurrentUser currentUser,
        IFileStorageService fileStorageService)
    {
        _users = database.GetCollection<User>("users");
        _currentUser = currentUser;
        _fileStorageService = fileStorageService;
    }

    public async Task<Result<UploadAvatarResponse>> Handle(
        UploadAvatarCommand request, CancellationToken cancellationToken)
    {
        var user = await _users
            .Find(u => u.Id == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result<UploadAvatarResponse>.Failure("User not found.");

        if (request.FileStream is null || request.FileStream.Length == 0)
            return Result<UploadAvatarResponse>.Failure("File stream is empty.");

        // Delete old avatar file if present
        if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            await _fileStorageService.DeleteFileAsync(user.AvatarUrl, cancellationToken);
        }

        // Save new avatar
        var relativeUrl = await _fileStorageService.SaveFileAsync(
            request.FileStream,
            request.FileName,
            "uploads/avatars",
            cancellationToken);

        user.AvatarUrl = relativeUrl;
        user.ModifiedAt = DateTime.UtcNow;

        await _users.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: cancellationToken);

        return Result<UploadAvatarResponse>.Success(new UploadAvatarResponse(relativeUrl));
    }
}
