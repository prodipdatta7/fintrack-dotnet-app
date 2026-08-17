using System.Text.RegularExpressions;
using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Categories.Domain;
using FinTrack.Modules.Categories.Features.GetTags;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Categories.Features.CreateTag;

internal sealed class CreateTagHandler : IRequestHandler<CreateTagCommand, Result<TagDto>>
{
    private readonly IMongoCollection<UserTag> _tags;
    private readonly ICurrentUser _currentUser;

    public CreateTagHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _tags = database.GetCollection<UserTag>("tags");
        _currentUser = currentUser;
    }

    public async Task<Result<TagDto>> Handle(CreateTagCommand request, CancellationToken ct)
    {
        var name = request.Name.Trim().TrimStart('#');
        if (string.IsNullOrWhiteSpace(name))
            return Result<TagDto>.Failure("Tag name is required.");

        var normalized = NameKeys.Normalize(name);

        var existing = await FindByNormalizedName(normalized, ct);
        if (existing is not null)
            return Result<TagDto>.Success(new TagDto(existing.Id, existing.Name));

        var tag = new UserTag
        {
            Name = name,
            NormalizedName = normalized,
            UserId = _currentUser.UserId,
            CreatedBy = _currentUser.Email
        };

        try
        {
            await _tags.InsertOneAsync(tag, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // A concurrent request created the same tag first; return it instead.
            existing = await FindByNormalizedName(normalized, ct);
            if (existing is not null)
                return Result<TagDto>.Success(new TagDto(existing.Id, existing.Name));
            throw;
        }

        return Result<TagDto>.Success(new TagDto(tag.Id, tag.Name));
    }

    private async Task<UserTag?> FindByNormalizedName(string normalized, CancellationToken ct)
    {
        var filter = Builders<UserTag>.Filter.Eq(t => t.UserId, _currentUser.UserId)
            & Builders<UserTag>.Filter.Eq(t => t.NormalizedName, normalized);

        return await _tags.Find(filter).FirstOrDefaultAsync(ct);
    }
}