using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Categories.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Categories.Features.UpdateCategory;

internal sealed class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, Result>
{
    private readonly IMongoCollection<Category> _categories;
    private readonly ICurrentUser _currentUser;

    public UpdateCategoryHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _categories = database.GetCollection<Category>("categories");
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categories
            .Find(c => c.Id == request.Id && c.UserId == _currentUser.UserId && !c.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null)
            return Result.Failure("Category not found or default categories cannot be modified.");

        var update = Builders<Category>.Update
            .Set(c => c.Name, request.Name)
            .Set(c => c.Type, request.Type)
            .Set(c => c.Icon, request.Icon)
            .Set(c => c.Color, request.Color)
            .Set(c => c.BudgetLimit, request.BudgetLimit)
            .Set(c => c.ModifiedAt, DateTime.UtcNow);

        await _categories.UpdateOneAsync(
            c => c.Id == request.Id && c.UserId == _currentUser.UserId,
            update,
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}
