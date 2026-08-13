using FinTrack.BuildingBlocks;
using FinTrack.Contracts.IntegrationEvents;
using FinTrack.Modules.Users.Domain;
using FinTrack.Modules.Users.Services;
using MassTransit;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Users.Features.Register;

internal sealed class RegisterHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private readonly IMongoCollection<User> _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPublishEndpoint _publishEndpoint;

    public RegisterHandler(
        IMongoDatabase database,
        IPasswordHasher passwordHasher,
        IPublishEndpoint publishEndpoint)
    {
        _users = database.GetCollection<User>("users");
        _passwordHasher = passwordHasher;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<RegisterResponse>> Handle(
        RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _users
            .Find(u => u.Email == request.Email.ToLowerInvariant())
            .FirstOrDefaultAsync(cancellationToken);

        if (existingUser is not null)
            return Result<RegisterResponse>.Failure("A user with this email already exists.");

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserId = string.Empty, // Self-owned; UserId set after insert
            CreatedBy = "system"
        };
        user.UserId = user.Id;

        await _users.InsertOneAsync(user, cancellationToken: cancellationToken);

        await _publishEndpoint.Publish(
            new UserRegistered(user.Id, user.Email, user.CreatedAt),
            cancellationToken);

        return Result<RegisterResponse>.Success(new RegisterResponse(user.Id, user.Email));
    }
}
