using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.ProposeCreateTag;

internal sealed class ProposeCreateTagHandler
    : IRequestHandler<ProposeCreateTagCommand, Result<ProposedActionDto<ProposedCreateTagPayload>>>
{
    private readonly IMongoCollection<BsonDocument> _tags;
    private readonly ICurrentUser _currentUser;

    public ProposeCreateTagHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _tags = database.GetCollection<BsonDocument>("tags");
        _currentUser = currentUser;
    }

    public async Task<Result<ProposedActionDto<ProposedCreateTagPayload>>> Handle(
        ProposeCreateTagCommand request, CancellationToken ct)
    {
        var raw = request.Name.Trim().TrimStart('#');
        if (string.IsNullOrWhiteSpace(raw))
            return Result<ProposedActionDto<ProposedCreateTagPayload>>.Failure("Tag name is required.");

        var normalized = raw.ToLowerInvariant();

        var existing = await _tags.Find(
            Builders<BsonDocument>.Filter.Eq("UserId", _currentUser.UserId) &
            Builders<BsonDocument>.Filter.Eq("NormalizedName", normalized)
        ).FirstOrDefaultAsync(ct);

        var alreadyExists = existing != null;
        var summary = alreadyExists
            ? $"Tag \"#{raw}\" already exists."
            : $"Create a new tag \"#{raw}\".";

        var payload = new ProposedCreateTagPayload(
            Name: raw,
            NormalizedName: normalized,
            AlreadyExists: alreadyExists);

        var proposedAction = ProposedActionDto<ProposedCreateTagPayload>.Create(
            actionType: "AddTag",
            summary: summary,
            payload: payload);

        return Result<ProposedActionDto<ProposedCreateTagPayload>>.Success(proposedAction);
    }
}
