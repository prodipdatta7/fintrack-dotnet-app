using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Categories.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Categories.Features.GetCategories;

internal sealed class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, Result<List<CategoryDto>>>
{
    private readonly IMongoCollection<Category> _categories;
    private readonly ICurrentUser _currentUser;

    public GetCategoriesHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _categories = database.GetCollection<Category>("categories");
        _currentUser = currentUser;
    }

    public async Task<Result<List<CategoryDto>>> Handle(
        GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var builder = Builders<Category>.Filter;
        // Defaults are seeded per user with IsDefault=true, so UserId alone scopes correctly;
        // an OR on IsDefault would leak every user's seeded defaults to everyone.
        var filter = builder.Eq(c => c.UserId, _currentUser.UserId);

        if (request.Type.HasValue)
            filter &= builder.Eq(c => c.Type, request.Type.Value);

        var categories = await _categories.Find(filter)
            .SortBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var dtos = categories.Select(c => new CategoryDto(
            c.Id,
            c.Name,
            c.Type,
            c.Icon,
            c.Color,
            c.IsDefault,
            c.BudgetLimit)).ToList();

        return Result<List<CategoryDto>>.Success(dtos);
    }
}
