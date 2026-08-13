using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Dashboard.Features.GetCashflowSeries;

internal sealed class GetCashflowSeriesHandler
    : IRequestHandler<GetCashflowSeriesQuery, Result<IReadOnlyList<CashflowPointDto>>>
{
    private readonly IMongoCollection<BsonDocument> _transactions;
    private readonly ICurrentUser _currentUser;
    private static bool _indexCreated;

    public GetCashflowSeriesHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _transactions = database.GetCollection<BsonDocument>("transactions");
        _currentUser = currentUser;

        if (!_indexCreated)
        {
            // TASK-028 / RISK-001: same { UserId: 1, date: -1 } index as the summary handler —
            // CreateOne is idempotent for an identical spec, whichever handler runs first wins.
            var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("UserId").Descending("date");
            _transactions.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(indexKeys));
            _indexCreated = true;
        }
    }

    public async Task<Result<IReadOnlyList<CashflowPointDto>>> Handle(
        GetCashflowSeriesQuery request, CancellationToken cancellationToken)
    {
        // GetCashflowSeriesValidator has already run via the ValidationBehavior pipeline:
        // Timeframe is in the allowed set and Custom carries a valid From/To pair.
        var plan = CashflowBuckets.Build(request.Timeframe, request.From, request.To, DateTime.UtcNow);

        var stages = GetCashflowSeriesPipeline.BuildStages(_currentUser.UserId, plan, request.AccountId);
        var pipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(stages);

        var groups = await _transactions
            .Aggregate(pipeline, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);

        var series = GetCashflowSeriesPipeline.MapSeries(plan, groups);

        return Result<IReadOnlyList<CashflowPointDto>>.Success(series);
    }
}
