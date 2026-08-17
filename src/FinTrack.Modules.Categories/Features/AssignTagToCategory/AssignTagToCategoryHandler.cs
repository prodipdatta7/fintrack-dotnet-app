using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Categories.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Categories.Features.AssignTagToCategory;

internal sealed class AssignTagToCategoryHandler : IRequestHandler<AssignTagToCategoryCommand, Result>
{
    private readonly IMongoCollection<Category> _categories;
    private readonly IMongoCollection<UserTag> _tags;
    private readonly IMongoCollection<CategoryTag> _categoryTags;
    private readonly ICurrentUser _currentUser;

    public AssignTagToCategoryHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _categories = database.GetCollection<Category>("categories");
        _tags = database.GetCollection<UserTag>("tags");
        _categoryTags = database.GetCollection<CategoryTag>("category_tags");
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(AssignTagToCategoryCommand request, CancellationToken ct)
    {
        var category = await _categories
            .Find(c => c.Id == request.CategoryId && c.UserId == _currentUser.UserId)
            .FirstOrDefaultAsync(ct);

        if (category is null)
            return Result.Failure("Category not found.");

        var name = request.Tag.Trim().TrimStart('#');
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Tag name is required.");

        var tag = await FindOrCreateTag(name, ct);

        var existing = await _categoryTags
            .Find(t => t.CategoryId == request.CategoryId && t.TagId == tag.Id)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
            return Result.Success();

        await _categoryTags.InsertOneAsync(new CategoryTag
        {
            CategoryId = request.CategoryId,
            TagId = tag.Id,
            Name = tag.Name,
            UserId = _currentUser.UserId,
            CreatedBy = _currentUser.Email
        }, cancellationToken: ct);

        return Result.Success();
    }

    private async Task<UserTag> FindOrCreateTag(string name, CancellationToken ct)
    {
        var normalized = NameKeys.Normalize(name);

        var existing = await _tags
            .Find(t => t.UserId == _currentUser.UserId && t.NormalizedName == normalized)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
            return existing;

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
            return tag;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return await _tags
                .Find(t => t.UserId == _currentUser.UserId && t.NormalizedName == normalized)
                .FirstAsync(ct);
        }
    }
}