using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Contracts.IntegrationEvents;
using FinTrack.Modules.Categories.Domain;
using MassTransit;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Categories.Features.CreateCategory;

internal sealed class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Result<string>>
{
    private readonly IMongoCollection<Category> _categories;
    private readonly ICurrentUser _currentUser;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreateCategoryHandler(
        IMongoDatabase database,
        ICurrentUser currentUser,
        IPublishEndpoint publishEndpoint)
    {
        _categories = database.GetCollection<Category>("categories");
        _currentUser = currentUser;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<string>> Handle(
        CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Result<string>.Failure("Category name is required.");

        var normalized = NameKeys.Normalize(name);

        var existing = await _categories
            .Find(c => c.UserId == _currentUser.UserId && c.NormalizedName == normalized)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
            return Result<string>.Failure($"A category named \"{existing.Name}\" already exists.");

        var category = new Category
        {
            Name = name,
            NormalizedName = normalized,
            Type = request.Type,
            Icon = request.Icon,
            Color = request.Color,
            BudgetLimit = request.BudgetLimit,
            IsDefault = false,
            UserId = _currentUser.UserId,
            CreatedBy = _currentUser.Email
        };

        try
        {
            await _categories.InsertOneAsync(category, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return Result<string>.Failure($"A category named \"{name}\" already exists.");
        }

        await _publishEndpoint.Publish(new CategoryCreated(
            category.Id,
            category.UserId,
            category.Name,
            category.Type.ToString()), cancellationToken);

        return Result<string>.Success(category.Id);
    }
}