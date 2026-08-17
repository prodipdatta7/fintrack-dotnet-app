using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Categories.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Categories.Features.UnassignTagFromCategory;

internal sealed class UnassignTagFromCategoryHandler : IRequestHandler<UnassignTagFromCategoryCommand, Result>
{
    private readonly IMongoCollection<UserTag> _tags;
    private readonly IMongoCollection<CategoryTag> _categoryTags;
    private readonly ICurrentUser _currentUser;

    public UnassignTagFromCategoryHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _tags = database.GetCollection<UserTag>("tags");
        _categoryTags = database.GetCollection<CategoryTag>("category_tags");
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UnassignTagFromCategoryCommand request, CancellationToken ct)
    {
        var name = request.Tag.Trim().TrimStart('#');
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Tag name is required.");

        var normalized = NameKeys.Normalize(name);

        var tag = await _tags
            .Find(t => t.UserId == _currentUser.UserId && t.NormalizedName == normalized)
            .FirstOrDefaultAsync(ct);

        if (tag is null)
            return Result.Success();

        await _categoryTags.DeleteOneAsync(
            t => t.CategoryId == request.CategoryId
                && t.TagId == tag.Id
                && t.UserId == _currentUser.UserId,
            cancellationToken: ct);

        return Result.Success();
    }
}