using FinTrack.Modules.Users.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Entities;
using DomainRefreshToken = FinTrack.Modules.Users.Domain.RefreshToken;

namespace FinTrack.Modules.Users.Features.Login;

internal sealed class LoginHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly ILogger<LoginHandler> _logger;
    private readonly JwtOptions _jwtOptions;

    public LoginHandler(ILogger<LoginHandler> logger, IOptions<JwtOptions> jwtOptions)
    {
        _logger = logger;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.ToLowerInvariant().Trim();

        var user = await DB.Find<User>()
            .Match(u => u.Email == normalizedEmail)
            .ExecuteFirstAsync(cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var accessToken = JwtTokenGenerator.GenerateAccessToken(user.ID, user.Email, _jwtOptions);
        var refreshToken = JwtTokenGenerator.GenerateRefreshToken();

        // Revoke existing refresh tokens for this user
        await DB.Update<DomainRefreshToken>()
            .Match(rt => rt.UserId == user.ID && !rt.IsRevoked)
            .Modify(rt => rt.IsRevoked, true)
            .ExecuteAsync(cancellationToken);

        // Store new refresh token
        var refreshTokenEntity = new DomainRefreshToken
        {
            UserId = user.ID,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            IsRevoked = false
        };
        await refreshTokenEntity.SaveAsync(cancellation: cancellationToken);

        _logger.LogInformation("User {UserId} logged in", user.ID);

        return new LoginResult(user.ID, user.Email, accessToken, refreshToken);
    }
}
