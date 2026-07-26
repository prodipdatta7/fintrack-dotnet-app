using FinTrack.BuildingBlocks;
using FinTrack.Modules.Budgets.Domain;
using MediatR;
using MongoDB.Entities;

namespace FinTrack.Modules.Budgets.Features.CreateBudget;

public record CreateBudgetCommand(
    string CategoryId,
    string Name,
    decimal Limit,
    string Period,
    DateTime StartDate,
    DateTime EndDate) : IRequest<string>;

internal sealed class CreateBudgetHandler : IRequestHandler<CreateBudgetCommand, string>
{
    private readonly ICurrentUser _currentUser;

    public CreateBudgetHandler(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task<string> Handle(CreateBudgetCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User must be authenticated.");

        var budget = new Budget
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Name = request.Name,
            Limit = request.Limit,
            CurrentSpend = 0,
            Period = request.Period,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedBy = userId,
            CreateDate = DateTime.UtcNow
        };

        await budget.SaveAsync(cancellation: cancellationToken);
        return budget.ID;
    }
}
