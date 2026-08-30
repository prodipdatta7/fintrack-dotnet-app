using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.ProposeCreateCategory;

internal sealed class ProposeCreateCategoryHandler
    : IRequestHandler<ProposeCreateCategoryCommand, Result<ProposedActionDto<ProposedCreateCategoryPayload>>>
{
    private readonly IMongoCollection<BsonDocument> _categories;
    private readonly ICurrentUser _currentUser;

    public ProposeCreateCategoryHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _categories = database.GetCollection<BsonDocument>("categories");
        _currentUser = currentUser;
    }

    public async Task<Result<ProposedActionDto<ProposedCreateCategoryPayload>>> Handle(
        ProposeCreateCategoryCommand request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        var type = string.Equals(request.Type?.Trim(), "Income", StringComparison.OrdinalIgnoreCase) ? "Income" : "Expense";
        var icon = string.IsNullOrWhiteSpace(request.Icon) ? "tag" : request.Icon.Trim();
        var color = string.IsNullOrWhiteSpace(request.Color) ? "#6366f1" : request.Color.Trim();
        var budgetLimit = request.BudgetLimit ?? 0m;

        var normalized = name.ToLowerInvariant();
        var existing = await _categories.Find(
            Builders<BsonDocument>.Filter.Eq("UserId", _currentUser.UserId) &
            Builders<BsonDocument>.Filter.Eq("NormalizedName", normalized)
        ).FirstOrDefaultAsync(ct);

        var alreadyExists = existing != null;
        var summary = alreadyExists
            ? $"Category \"{name}\" already exists ({type}). Re-create or edit category \"{name}\"?"
            : budgetLimit > 0m
                ? $"Create a new {type.ToLowerInvariant()} category \"{name}\" with a monthly budget limit of ৳{budgetLimit:N0}."
                : $"Create a new {type.ToLowerInvariant()} category \"{name}\".";

        var payload = new ProposedCreateCategoryPayload(
            Name: name,
            Type: type,
            Icon: icon,
            Color: color,
            BudgetLimit: budgetLimit,
            AlreadyExists: alreadyExists);

        var proposedAction = ProposedActionDto<ProposedCreateCategoryPayload>.Create(
            actionType: "AddCategory",
            summary: summary,
            payload: payload);

        return Result<ProposedActionDto<ProposedCreateCategoryPayload>>.Success(proposedAction);
    }
}
