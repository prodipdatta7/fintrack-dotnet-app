using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Categories.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Categories.Features.GetTags;

internal sealed class GetTagsHandler : IRequestHandler<GetTagsQuery, Result<List<TagDto>>>
{
    private readonly IMongoCollection<UserTag> _tags;
    private readonly ICurrentUser _currentUser;

    public GetTagsHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _tags = database.GetCollection<UserTag>("tags");
        _currentUser = currentUser;
    }

    public async Task<Result<List<TagDto>>> Handle(GetTagsQuery request, CancellationToken ct)
    {
        var tags = await _tags
            .Find(t => t.UserId == _currentUser.UserId)
            .SortBy(t => t.Name)
            .ToListAsync(ct);

        var dtos = tags.Select(t => new TagDto(t.Id, t.Name)).ToList();

        return Result<List<TagDto>>.Success(dtos);
    }
}
