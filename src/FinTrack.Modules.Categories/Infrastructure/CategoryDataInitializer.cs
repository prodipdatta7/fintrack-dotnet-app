using FinTrack.Modules.Categories.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Categories.Infrastructure;

/// <summary>
/// One-time startup migration that backfills the case-insensitive uniqueness
/// keys on legacy documents, deduplicates existing records, and creates the
/// unique indexes that enforce "one category/tag name per user".
/// </summary>
public sealed class CategoryDataInitializer : IHostedService
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Category> _categories;
    private readonly IMongoCollection<UserTag> _tags;
    private readonly IMongoCollection<CategoryTag> _categoryTags;
    private readonly ILogger<CategoryDataInitializer> _logger;

    public CategoryDataInitializer(IMongoDatabase database, ILogger<CategoryDataInitializer> logger)
    {
        _database = database;
        _categories = database.GetCollection<Category>("categories");
        _tags = database.GetCollection<UserTag>("tags");
        _categoryTags = database.GetCollection<CategoryTag>("category_tags");
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await BackfillCategoryKeysAsync(cancellationToken);
        await BackfillTagKeysAsync(cancellationToken);
        await BackfillCategoryTagIdsAsync(cancellationToken);
        await DeduplicateCategoriesAsync(cancellationToken);
        await DeduplicateTagsAsync(cancellationToken);
        await DeduplicateCategoryTagsAsync(cancellationToken);
        await CreateUniqueIndexesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task BackfillCategoryKeysAsync(CancellationToken ct)
    {
        var filter = Builders<Category>.Filter.Or(
            Builders<Category>.Filter.Exists(c => c.NormalizedName, false),
            Builders<Category>.Filter.Eq(c => c.NormalizedName, string.Empty));

        var missing = await _categories.Find(filter).ToListAsync(ct);
        foreach (var category in missing)
        {
            await _categories.UpdateOneAsync(
                Builders<Category>.Filter.Eq(c => c.Id, category.Id),
                Builders<Category>.Update.Set(c => c.NormalizedName, NameKeys.Normalize(category.Name)),
                cancellationToken: ct);
        }

        if (missing.Count > 0)
            _logger.LogInformation("Backfilled NormalizedName for {Count} categories.", missing.Count);
    }

    private async Task BackfillTagKeysAsync(CancellationToken ct)
    {
        var filter = Builders<UserTag>.Filter.Or(
            Builders<UserTag>.Filter.Exists(t => t.NormalizedName, false),
            Builders<UserTag>.Filter.Eq(t => t.NormalizedName, string.Empty));

        var missing = await _tags.Find(filter).ToListAsync(ct);
        foreach (var tag in missing)
        {
            await _tags.UpdateOneAsync(
                Builders<UserTag>.Filter.Eq(t => t.Id, tag.Id),
                Builders<UserTag>.Update.Set(t => t.NormalizedName, NameKeys.Normalize(tag.Name)),
                cancellationToken: ct);
        }

        if (missing.Count > 0)
            _logger.LogInformation("Backfilled NormalizedName for {Count} tags.", missing.Count);
    }

    private async Task BackfillCategoryTagIdsAsync(CancellationToken ct)
    {
        var filter = Builders<CategoryTag>.Filter.Or(
            Builders<CategoryTag>.Filter.Exists(t => t.TagId, false),
            Builders<CategoryTag>.Filter.Eq(t => t.TagId, string.Empty));

        var missing = await _categoryTags.Find(filter).ToListAsync(ct);
        foreach (var join in missing)
        {
            var normalized = NameKeys.Normalize(join.Name);
            var tag = await _tags
                .Find(t => t.UserId == join.UserId && t.NormalizedName == normalized)
                .FirstOrDefaultAsync(ct);

            if (tag is null)
            {
                tag = new UserTag
                {
                    Name = join.Name,
                    NormalizedName = normalized,
                    UserId = join.UserId,
                    CreatedBy = "system"
                };
                await _tags.InsertOneAsync(tag, cancellationToken: ct);
            }

            await _categoryTags.UpdateOneAsync(
                Builders<CategoryTag>.Filter.Eq(t => t.Id, join.Id),
                Builders<CategoryTag>.Update.Set(t => t.TagId, tag.Id),
                cancellationToken: ct);
        }

        if (missing.Count > 0)
            _logger.LogInformation("Backfilled TagId for {Count} category-tag links.", missing.Count);
    }

    private async Task DeduplicateCategoriesAsync(CancellationToken ct)
    {
        var allCategories = await _categories.Find(_ => true).ToListAsync(ct);
        var duplicatesGrouped = allCategories
            .Where(c => !string.IsNullOrEmpty(c.NormalizedName))
            .GroupBy(c => new { c.UserId, c.NormalizedName })
            .Where(g => g.Count() > 1);

        var transactionsCollection = _database.GetCollection<BsonDocument>("transactions");

        foreach (var group in duplicatesGrouped)
        {
            var ordered = group.OrderByDescending(c => c.IsDefault).ThenBy(c => c.CreatedAt).ToList();
            var canonical = ordered.First();
            var toRemove = ordered.Skip(1).ToList();

            foreach (var dup in toRemove)
            {
                await _categoryTags.UpdateManyAsync(
                    Builders<CategoryTag>.Filter.Eq(t => t.CategoryId, dup.Id),
                    Builders<CategoryTag>.Update.Set(t => t.CategoryId, canonical.Id),
                    cancellationToken: ct);

                await transactionsCollection.UpdateManyAsync(
                    Builders<BsonDocument>.Filter.Eq("categoryId", dup.Id),
                    Builders<BsonDocument>.Update.Set("categoryId", canonical.Id),
                    cancellationToken: ct);

                await _categories.DeleteOneAsync(Builders<Category>.Filter.Eq(c => c.Id, dup.Id), cancellationToken: ct);
            }

            _logger.LogInformation("Deduplicated category '{Name}' for user '{UserId}'. Removed {Count} duplicate(s).",
                canonical.Name, group.Key.UserId, toRemove.Count);
        }
    }

    private async Task DeduplicateTagsAsync(CancellationToken ct)
    {
        var allTags = await _tags.Find(_ => true).ToListAsync(ct);
        var duplicatesGrouped = allTags
            .Where(t => !string.IsNullOrEmpty(t.NormalizedName))
            .GroupBy(t => new { t.UserId, t.NormalizedName })
            .Where(g => g.Count() > 1);

        foreach (var group in duplicatesGrouped)
        {
            var ordered = group.OrderBy(t => t.CreatedAt).ToList();
            var canonical = ordered.First();
            var toRemove = ordered.Skip(1).ToList();

            foreach (var dup in toRemove)
            {
                await _categoryTags.UpdateManyAsync(
                    Builders<CategoryTag>.Filter.Eq(t => t.TagId, dup.Id),
                    Builders<CategoryTag>.Update.Set(t => t.TagId, canonical.Id),
                    cancellationToken: ct);

                await _tags.DeleteOneAsync(Builders<UserTag>.Filter.Eq(t => t.Id, dup.Id), cancellationToken: ct);
            }

            _logger.LogInformation("Deduplicated tag '{Name}' for user '{UserId}'. Removed {Count} duplicate(s).",
                canonical.Name, group.Key.UserId, toRemove.Count);
        }
    }

    private async Task DeduplicateCategoryTagsAsync(CancellationToken ct)
    {
        var allJoins = await _categoryTags.Find(_ => true).ToListAsync(ct);
        var duplicatesGrouped = allJoins
            .Where(t => !string.IsNullOrEmpty(t.CategoryId) && !string.IsNullOrEmpty(t.TagId))
            .GroupBy(t => new { t.CategoryId, t.TagId })
            .Where(g => g.Count() > 1);

        foreach (var group in duplicatesGrouped)
        {
            var ordered = group.OrderBy(t => t.CreatedAt).ToList();
            var canonical = ordered.First();
            var toRemove = ordered.Skip(1).ToList();

            foreach (var dup in toRemove)
            {
                await _categoryTags.DeleteOneAsync(Builders<CategoryTag>.Filter.Eq(t => t.Id, dup.Id), cancellationToken: ct);
            }

            _logger.LogInformation("Deduplicated category-tag link for Category '{CategoryId}' and Tag '{TagId}'. Removed {Count} duplicate(s).",
                group.Key.CategoryId, group.Key.TagId, toRemove.Count);
        }
    }

    private async Task CreateUniqueIndexesAsync(CancellationToken ct)
    {
        await _categories.Indexes.CreateOneAsync(
            new CreateIndexModel<Category>(
                Builders<Category>.IndexKeys
                    .Ascending(c => c.UserId)
                    .Ascending(c => c.NormalizedName),
                new CreateIndexOptions { Unique = true, Name = "uq_categories_user_normalizedname" }),
            cancellationToken: ct);

        await _tags.Indexes.CreateOneAsync(
            new CreateIndexModel<UserTag>(
                Builders<UserTag>.IndexKeys
                    .Ascending(t => t.UserId)
                    .Ascending(t => t.NormalizedName),
                new CreateIndexOptions { Unique = true, Name = "uq_tags_user_normalizedname" }),
            cancellationToken: ct);

        await _categoryTags.Indexes.CreateOneAsync(
            new CreateIndexModel<CategoryTag>(
                Builders<CategoryTag>.IndexKeys
                    .Ascending(t => t.CategoryId)
                    .Ascending(t => t.TagId),
                new CreateIndexOptions { Unique = true, Name = "uq_categorytags_category_tag" }),
            cancellationToken: ct);

        _logger.LogInformation("Category/Tag unique indexes ensured.");
    }
}