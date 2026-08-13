using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Users.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Users.Features.UpdateProfile;

internal sealed class UpdateProfileHandler : IRequestHandler<UpdateProfileCommand, Result<UpdateProfileResponse>>
{
    private readonly IMongoCollection<User> _users;
    private readonly ICurrentUser _currentUser;

    public UpdateProfileHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _users = database.GetCollection<User>("users");
        _currentUser = currentUser;
    }

    public async Task<Result<UpdateProfileResponse>> Handle(
        UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _users
            .Find(u => u.Id == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result<UpdateProfileResponse>.Failure("User not found.");

        // Email is owned by Firebase and is read-only here — never update it from this endpoint.
        // AvatarUrl is owned by UploadAvatar — never accept it from this endpoint.
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.ModifiedAt = DateTime.UtcNow;

        await _users.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: cancellationToken);

        return Result<UpdateProfileResponse>.Success(new UpdateProfileResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.AvatarUrl));
    }
}
