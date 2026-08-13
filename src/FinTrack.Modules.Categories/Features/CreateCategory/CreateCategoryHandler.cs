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
        var category = new Category
        {
            Name = request.Name,
            Type = request.Type,
            Icon = request.Icon,
            Color = request.Color,
            BudgetLimit = request.BudgetLimit,
            IsDefault = false,
            UserId = _currentUser.UserId,
            CreatedBy = _currentUser.Email
        };

        await _categories.InsertOneAsync(category, cancellationToken: cancellationToken);

        await _publishEndpoint.Publish(new CategoryCreated(
            category.Id,
            category.UserId,
            category.Name,
            category.Type.ToString()), cancellationToken);

        return Result<string>.Success(category.Id);
    }
}
