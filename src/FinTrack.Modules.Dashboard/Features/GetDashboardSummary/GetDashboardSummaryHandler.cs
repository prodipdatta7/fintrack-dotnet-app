using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Dashboard.Features.GetDashboardSummary;

internal sealed class GetDashboardSummaryHandler
    : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    private readonly IMongoCollection<BsonDocument> _transactions;
    private readonly ICurrentUser _currentUser;
    private static bool _indexCreated;

    public GetDashboardSummaryHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _transactions = database.GetCollection<BsonDocument>("transactions");
        _currentUser = currentUser;

        if (!_indexCreated)
        {
            // TASK-028 / RISK-001: { UserId: 1, date: -1 } is the driving filter of every
            // dashboard aggregation. Element names match the Transactions module's mapping
            // (PascalCase UserId from AuditableEntity, camelCase date via [BsonElement]).
            var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("UserId").Descending("date");
            _transactions.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(indexKeys));
            _indexCreated = true;
        }
    }

    public async Task<Result<DashboardSummaryDto>> Handle(
        GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var stages = GetDashboardSummaryPipeline.BuildStages(
            _currentUser.UserId,
            GetDashboardSummaryPipeline.NormalizeUtc(request.From),
            GetDashboardSummaryPipeline.NormalizeUtc(request.To),
            request.AccountId);

        var pipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(stages);

        // $facet always emits exactly one document, even over an empty match.
        var facetResult = await _transactions
            .Aggregate(pipeline, cancellationToken: cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);

        var dto = facetResult is null
            ? GetDashboardSummaryPipeline.Empty
            : GetDashboardSummaryPipeline.Map(facetResult);

        return Result<DashboardSummaryDto>.Success(dto);
    }
}
