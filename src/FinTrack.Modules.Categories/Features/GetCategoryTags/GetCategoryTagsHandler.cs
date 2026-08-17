using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Categories.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Categories.Features.GetCategoryTags;

internal sealed class GetCategoryTagsHandler : IRequestHandler<GetCategoryTagsQuery, Result<List<string>>>
{
    private readonly IMongoCollection<CategoryTag> _categoryTags;
    private readonly ICurrentUser _currentUser;

    public GetCategoryTagsHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _categoryTags = database.GetCollection<CategoryTag>("category_tags");
        _currentUser = currentUser;
    }

    public async Task<Result<List<string>>> Handle(GetCategoryTagsQuery request, CancellationToken ct)
    {
        var tags = await _categoryTags
            .Find(t => t.CategoryId == request.CategoryId && t.UserId == _currentUser.UserId)
            .SortBy(t => t.Name)
            .ToListAsync(ct);

        var names = tags.Select(t => t.Name).ToList();

        return Result<List<string>>.Success(names);
    }
}
