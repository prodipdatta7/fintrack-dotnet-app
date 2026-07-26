using FinTrack.BuildingBlocks;
using FinTrack.Modules.Categories.Domain;
using MediatR;
using MongoDB.Entities;

namespace FinTrack.Modules.Categories.Features.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    CategoryType Type) : IRequest<string>;

internal sealed class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, string>
{
    private readonly ICurrentUser _currentUser;

    public CreateCategoryHandler(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task<string> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User must be authenticated.");

        var category = new Category
        {
            UserId = userId,
            Name = request.Name,
            Type = request.Type,
            CreatedBy = userId,
            CreateDate = DateTime.UtcNow
        };

        await category.SaveAsync(cancellation: cancellationToken);
        return category.ID;
    }
}
