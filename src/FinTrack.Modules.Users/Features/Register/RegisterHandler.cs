using FinTrack.Contracts.Users;
using FinTrack.Modules.Users.Domain;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Entities;
using DomainRefreshToken = FinTrack.Modules.Users.Domain.RefreshToken;

namespace FinTrack.Modules.Users.Features.Register;

internal sealed class RegisterHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<RegisterHandler> _logger;
    private readonly JwtOptions _jwtOptions;

    public RegisterHandler(
        IPublishEndpoint publishEndpoint,
        ILogger<RegisterHandler> logger,
        IOptions<JwtOptions> jwtOptions)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if user already exists
        var existingUser = await DB.Find<User>()
            .Match(u => u.Email == request.Email)
            .ExecuteFirstAsync(cancellationToken);

        if (existingUser is not null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var user = new User
        {
            Email = request.Email.ToLowerInvariant().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedBy = request.Email,
            CreateDate = DateTime.UtcNow,
            TimeZoneOffsetInMinutes = 0
        };

        await user.SaveAsync(cancellation: cancellationToken);

        // Generate tokens
        var accessToken = JwtTokenGenerator.GenerateAccessToken(user.ID, user.Email, _jwtOptions);
        var refreshToken = JwtTokenGenerator.GenerateRefreshToken();

        // Store refresh token
        var refreshTokenEntity = new DomainRefreshToken
        {
            UserId = user.ID,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            IsRevoked = false
        };
        await refreshTokenEntity.SaveAsync(cancellation: cancellationToken);

        _logger.LogInformation("User {UserId} registered with email {Email}", user.ID, user.Email);

        await _publishEndpoint.Publish(new UserRegisteredEvent(user.ID, user.Email, DateTime.UtcNow), cancellationToken);

        return new RegisterResult(user.ID, user.Email, accessToken, refreshToken);
    }
}
