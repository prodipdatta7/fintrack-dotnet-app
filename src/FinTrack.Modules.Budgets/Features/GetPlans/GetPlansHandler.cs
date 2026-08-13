using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Budgets.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Budgets.Features.GetPlans;

internal sealed class GetPlansHandler : IRequestHandler<GetPlansQuery, Result<List<PlanDto>>>
{
    private readonly IMongoCollection<SavingsPlan> _plans;
    private readonly ICurrentUser _currentUser;
    private static bool _indexCreated;

    public GetPlansHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _plans = database.GetCollection<SavingsPlan>("savings_plans");
        _currentUser = currentUser;

        if (!_indexCreated)
        {
            var indexKeys = Builders<SavingsPlan>.IndexKeys.Ascending(p => p.UserId);
            _plans.Indexes.CreateOne(new CreateIndexModel<SavingsPlan>(indexKeys));
            _indexCreated = true;
        }
    }

    public async Task<Result<List<PlanDto>>> Handle(
        GetPlansQuery request, CancellationToken cancellationToken)
    {
        var filter = Builders<SavingsPlan>.Filter.Eq(p => p.UserId, _currentUser.UserId);

        var plans = await _plans.Find(filter)
            .SortBy(p => p.Deadline)
            .ToListAsync(cancellationToken);

        var items = plans.Select(p => new PlanDto(
            p.Id,
            p.Title,
            p.TargetAmount,
            p.CurrentAmount,
            p.Color,
            p.Deadline)).ToList();

        return Result<List<PlanDto>>.Success(items);
    }
}
