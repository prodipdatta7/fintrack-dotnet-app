using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Budgets.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Budgets.Features.UpdatePlan;

internal sealed class UpdatePlanHandler : IRequestHandler<UpdatePlanCommand, Result>
{
    private readonly IMongoCollection<SavingsPlan> _plans;
    private readonly ICurrentUser _currentUser;

    public UpdatePlanHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _plans = database.GetCollection<SavingsPlan>("savings_plans");
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        UpdatePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _plans
            .Find(p => p.Id == request.Id && p.UserId == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (plan is null)
            return Result.Failure("Plan not found.");

        var update = Builders<SavingsPlan>.Update
            .Set(p => p.Title, request.Title)
            .Set(p => p.TargetAmount, request.TargetAmount)
            .Set(p => p.CurrentAmount, request.CurrentAmount)
            .Set(p => p.Color, request.Color)
            .Set(p => p.Deadline, request.Deadline)
            .Set(p => p.ModifiedAt, DateTime.UtcNow);

        await _plans.UpdateOneAsync(
            p => p.Id == request.Id && p.UserId == _currentUser.UserId,
            update,
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}
