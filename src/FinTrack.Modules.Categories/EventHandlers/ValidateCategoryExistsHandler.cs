using FinTrack.Contracts.Queries;
using FinTrack.Modules.Categories.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Categories.EventHandlers;

internal sealed class ValidateCategoryExistsHandler : IRequestHandler<ValidateCategoryExistsQuery, bool>
{
    private readonly IMongoCollection<Category> _categories;

    public ValidateCategoryExistsHandler(IMongoDatabase database)
    {
        _categories = database.GetCollection<Category>("categories");
    }

    public async Task<bool> Handle(ValidateCategoryExistsQuery request, CancellationToken cancellationToken)
    {
        return await _categories
            .Find(c => c.Id == request.CategoryId && (c.UserId == request.UserId || c.IsDefault))
            .AnyAsync(cancellationToken);
    }
}
