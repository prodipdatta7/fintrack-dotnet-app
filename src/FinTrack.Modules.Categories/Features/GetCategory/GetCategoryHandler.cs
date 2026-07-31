using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Categories.Domain;
using FinTrack.Modules.Categories.Features.GetCategories;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Categories.Features.GetCategory;

internal sealed class GetCategoryHandler : IRequestHandler<GetCategoryQuery, Result<CategoryDto>>
{
    private readonly IMongoCollection<Category> _categories;
    private readonly ICurrentUser _currentUser;

    public GetCategoryHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _categories = database.GetCollection<Category>("categories");
        _currentUser = currentUser;
    }

    public async Task<Result<CategoryDto>> Handle(
        GetCategoryQuery request, CancellationToken cancellationToken)
    {
        var category = await _categories
            .Find(c => c.Id == request.Id && (c.UserId == _currentUser.UserId || c.UserId == string.Empty))
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null)
            return Result<CategoryDto>.Failure("Category not found.");

        var dto = new CategoryDto(
            category.Id,
            category.Name,
            category.Type,
            category.Icon,
            category.Color,
            category.UserId == string.Empty);

        return Result<CategoryDto>.Success(dto);
    }
}
