using FinTrack.BuildingBlocks;
using FinTrack.Modules.Users.Domain;
using FinTrack.Modules.Users.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace FinTrack.Modules.Users.Features.Login;

using RefreshTokenEntity = Domain.RefreshToken;

internal sealed class LoginHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<RefreshTokenEntity> _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public LoginHandler(
        IMongoDatabase database,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IConfiguration configuration)
    {
        _users = database.GetCollection<User>("users");
        _refreshTokens = database.GetCollection<RefreshTokenEntity>("refresh_tokens");
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _users
            .Find(u => u.Email == request.Email.ToLowerInvariant())
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<LoginResponse>.Failure("Invalid email or password.");

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshTokenDays = int.TryParse(_configuration["Jwt:RefreshTokenDays"], out var d) ? d : 7;

        var refreshToken = new RefreshTokenEntity
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays)
        };

        await _refreshTokens.InsertOneAsync(refreshToken, cancellationToken: cancellationToken);

        var accessTokenMinutes = int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var m) ? m : 15;

        return Result<LoginResponse>.Success(new LoginResponse(
            accessToken,
            refreshTokenValue,
            DateTime.UtcNow.AddMinutes(accessTokenMinutes)));
    }
}
