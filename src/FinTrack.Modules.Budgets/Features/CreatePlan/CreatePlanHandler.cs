using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Budgets.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Budgets.Features.CreatePlan;

internal sealed class CreatePlanHandler : IRequestHandler<CreatePlanCommand, Result<string>>
{
    private readonly IMongoCollection<SavingsPlan> _plans;
    private readonly ICurrentUser _currentUser;

    public CreatePlanHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _plans = database.GetCollection<SavingsPlan>("savings_plans");
        _currentUser = currentUser;
    }

    public async Task<Result<string>> Handle(
        CreatePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = new SavingsPlan
        {
            Title = request.Title,
            TargetAmount = request.TargetAmount,
            CurrentAmount = request.CurrentAmount,
            Color = request.Color,
            Deadline = request.Deadline,
            UserId = _currentUser.UserId,
            CreatedBy = _currentUser.Email
        };

        await _plans.InsertOneAsync(plan, cancellationToken: cancellationToken);

        return Result<string>.Success(plan.Id);
    }
}
