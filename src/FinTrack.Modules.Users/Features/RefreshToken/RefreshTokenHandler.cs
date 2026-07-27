using FinTrack.BuildingBlocks;
using FinTrack.Modules.Users.Domain;
using FinTrack.Modules.Users.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace FinTrack.Modules.Users.Features.RefreshToken;

using RefreshTokenEntity = Domain.RefreshToken;

internal sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<RefreshTokenEntity> _refreshTokens;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public RefreshTokenHandler(
        IMongoDatabase database,
        ITokenService tokenService,
        IConfiguration configuration)
    {
        _users = database.GetCollection<User>("users");
        _refreshTokens = database.GetCollection<RefreshTokenEntity>("refresh_tokens");
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(
        RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await _refreshTokens
            .Find(rt => rt.Token == request.RefreshToken && !rt.IsRevoked)
            .FirstOrDefaultAsync(cancellationToken);

        if (storedToken is null || storedToken.ExpiresAt < DateTime.UtcNow)
            return Result<RefreshTokenResponse>.Failure("Invalid or expired refresh token.");

        // Revoke the used token (rotate)
        var revokeUpdate = Builders<RefreshTokenEntity>.Update.Set(rt => rt.IsRevoked, true);
        await _refreshTokens.UpdateOneAsync(
            rt => rt.Id == storedToken.Id, revokeUpdate, cancellationToken: cancellationToken);

        var user = await _users
            .Find(u => u.Id == storedToken.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result<RefreshTokenResponse>.Failure("User not found.");

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshTokenDays = int.TryParse(_configuration["Jwt:RefreshTokenDays"], out var d) ? d : 7;

        var newRefreshToken = new RefreshTokenEntity
        {
            UserId = user.Id,
            Token = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays)
        };

        await _refreshTokens.InsertOneAsync(newRefreshToken, cancellationToken: cancellationToken);

        var accessTokenMinutes = int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var m) ? m : 15;

        return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse(
            newAccessToken,
            newRefreshTokenValue,
            DateTime.UtcNow.AddMinutes(accessTokenMinutes)));
    }
}
