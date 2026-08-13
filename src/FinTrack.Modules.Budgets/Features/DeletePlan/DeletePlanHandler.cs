using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Budgets.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Budgets.Features.DeletePlan;

// Savings plans carry no ledger references, so a hard delete is safe
// (unlike accounts, which are only ever soft-closed).
internal sealed class DeletePlanHandler : IRequestHandler<DeletePlanCommand, Result>
{
    private readonly IMongoCollection<SavingsPlan> _plans;
    private readonly ICurrentUser _currentUser;

    public DeletePlanHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _plans = database.GetCollection<SavingsPlan>("savings_plans");
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        DeletePlanCommand request, CancellationToken cancellationToken)
    {
        var builder = Builders<SavingsPlan>.Filter;
        var filter = builder.Eq(p => p.Id, request.Id)
            & builder.Eq(p => p.UserId, _currentUser.UserId);

        var deleteResult = await _plans.DeleteOneAsync(filter, cancellationToken);

        return deleteResult.DeletedCount == 0
            ? Result.Failure("Plan not found.")
            : Result.Success();
    }
}
