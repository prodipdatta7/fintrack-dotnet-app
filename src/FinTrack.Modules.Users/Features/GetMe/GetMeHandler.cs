using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Users.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Users.Features.GetMe;

internal sealed class GetMeHandler : IRequestHandler<GetMeQuery, Result<GetMeResponse>>
{
    private readonly IMongoCollection<User> _users;
    private readonly ICurrentUser _currentUser;

    public GetMeHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _users = database.GetCollection<User>("users");
        _currentUser = currentUser;
    }

    public async Task<Result<GetMeResponse>> Handle(
        GetMeQuery request, CancellationToken cancellationToken)
    {
        var user = await _users
            .Find(u => u.Id == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result<GetMeResponse>.Failure("User not found.");

        return Result<GetMeResponse>.Success(new GetMeResponse(
            user.Id, user.Email, user.FirstName, user.LastName, user.AvatarUrl));
    }
}
