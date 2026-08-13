using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Users.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Users.Features.LogoutAllSessions;

internal sealed class LogoutAllSessionsHandler : IRequestHandler<LogoutAllSessionsCommand, Result<LogoutAllSessionsResponse>>
{
    private readonly IMongoCollection<Domain.RefreshToken> _refreshTokens;
    private readonly ICurrentUser _currentUser;

    public LogoutAllSessionsHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _refreshTokens = database.GetCollection<Domain.RefreshToken>("refresh_tokens");
        _currentUser = currentUser;
    }

    public async Task<Result<LogoutAllSessionsResponse>> Handle(
        LogoutAllSessionsCommand request, CancellationToken cancellationToken)
    {
        var update = Builders<Domain.RefreshToken>.Update.Set(rt => rt.IsRevoked, true);

        await _refreshTokens.UpdateManyAsync(
            rt => rt.UserId == _currentUser.UserId && !rt.IsRevoked,
            update,
            cancellationToken: cancellationToken);

        return Result<LogoutAllSessionsResponse>.Success(
            new LogoutAllSessionsResponse("Logged out from all sessions successfully."));
    }
}
