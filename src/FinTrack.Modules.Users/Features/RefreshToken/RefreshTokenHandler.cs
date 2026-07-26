using System.IdentityModel.Tokens.Jwt;
using FinTrack.Modules.Users.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Entities;

namespace FinTrack.Modules.Users.Features.TokenRefresh;

internal sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly ILogger<RefreshTokenHandler> _logger;
    private readonly JwtOptions _jwtOptions;

    public RefreshTokenHandler(ILogger<RefreshTokenHandler> logger, IOptions<JwtOptions> jwtOptions)
    {
        _logger = logger;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await DB.Find<Domain.RefreshToken>()
            .Match(rt => rt.Token == request.RefreshToken && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ExecuteFirstAsync(cancellationToken);

        if (storedToken is null)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        var user = await DB.Find<User>()
            .Match(u => u.ID == storedToken.UserId)
            .ExecuteFirstAsync(cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        storedToken.IsRevoked = true;
        await storedToken.SaveAsync(cancellation: cancellationToken);

        var newAccessToken = JwtTokenGenerator.GenerateAccessToken(user.ID, user.Email, _jwtOptions);
        var newRefreshToken = JwtTokenGenerator.GenerateRefreshToken();

        var newRefreshTokenEntity = new Domain.RefreshToken
        {
            UserId = user.ID,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            IsRevoked = false
        };
        await newRefreshTokenEntity.SaveAsync(cancellation: cancellationToken);

        _logger.LogInformation("Token refreshed for user {UserId}", user.ID);

        return new RefreshTokenResult(user.ID, user.Email, newAccessToken, newRefreshToken);
    }
}
