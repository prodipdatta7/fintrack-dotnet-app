using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Users.Domain;
using FinTrack.Modules.Users.Services;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Users.Features.ChangePassword;

internal sealed class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, Result<ChangePasswordResponse>>
{
    private readonly IMongoCollection<User> _users;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordHandler(
        IMongoDatabase database,
        ICurrentUser currentUser,
        IPasswordHasher passwordHasher)
    {
        _users = database.GetCollection<User>("users");
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<ChangePasswordResponse>> Handle(
        ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _users
            .Find(u => u.Id == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result<ChangePasswordResponse>.Failure("User not found.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result<ChangePasswordResponse>.Failure("Current password is incorrect.");

        if (_passwordHasher.Verify(request.NewPassword, user.PasswordHash))
            return Result<ChangePasswordResponse>.Failure("New password cannot be the same as current password.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.ModifiedAt = DateTime.UtcNow;

        await _users.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: cancellationToken);

        return Result<ChangePasswordResponse>.Success(
            new ChangePasswordResponse("Password changed successfully."));
    }
}
