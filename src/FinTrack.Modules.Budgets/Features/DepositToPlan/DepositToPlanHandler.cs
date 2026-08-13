using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Budgets.Domain;
using FinTrack.Modules.Budgets.Features.GetPlans;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Budgets.Features.DepositToPlan;

internal sealed class DepositToPlanHandler : IRequestHandler<DepositToPlanCommand, Result<PlanDto>>
{
    private readonly IMongoCollection<SavingsPlan> _plans;
    private readonly ICurrentUser _currentUser;

    public DepositToPlanHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _plans = database.GetCollection<SavingsPlan>("savings_plans");
        _currentUser = currentUser;
    }

    public async Task<Result<PlanDto>> Handle(
        DepositToPlanCommand request, CancellationToken cancellationToken)
    {
        var builder = Builders<SavingsPlan>.Filter;
        var filter = builder.Eq(p => p.Id, request.Id)
            & builder.Eq(p => p.UserId, _currentUser.UserId);

        // Atomic increment — two concurrent deposits can never lose an update.
        var update = Builders<SavingsPlan>.Update
            .Inc(p => p.CurrentAmount, request.Amount)
            .Set(p => p.ModifiedAt, DateTime.UtcNow);

        var plan = await _plans.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<SavingsPlan> { ReturnDocument = ReturnDocument.After },
            cancellationToken);

        if (plan is null)
            return Result<PlanDto>.Failure("Plan not found.");

        return Result<PlanDto>.Success(new PlanDto(
            plan.Id,
            plan.Title,
            plan.TargetAmount,
            plan.CurrentAmount,
            plan.Color,
            plan.Deadline));
    }
}
