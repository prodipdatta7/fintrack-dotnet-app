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

        // If email is changing, check for uniqueness
        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await _users
                .Find(u => u.Email == request.Email && u.Id != user.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingUser is not null)
                return Result<UpdateProfileResponse>.Failure("Email address is already in use by another account.");
        }

        user.Email = request.Email;
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
