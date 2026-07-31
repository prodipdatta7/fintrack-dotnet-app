using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Transactions.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Transactions.Features.GetTransactionEvents;

public sealed class GetTransactionEventsHandler
    : IRequestHandler<GetTransactionEventsQuery, Result<IReadOnlyList<TransactionEventDto>>>
{
    private readonly IMongoCollection<TransactionEvent> _events;
    private readonly ICurrentUser _currentUser;
    private static bool _indexCreated;

    public GetTransactionEventsHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _events = database.GetCollection<TransactionEvent>("transaction_events");
        _currentUser = currentUser;

        if (!_indexCreated)
        {
            var indexKeys = Builders<TransactionEvent>.IndexKeys
                .Ascending(e => e.TransactionId)
                .Ascending(e => e.UserId)
                .Descending(e => e.OccurredOnUtc);

            _events.Indexes.CreateOne(new CreateIndexModel<TransactionEvent>(indexKeys));
            _indexCreated = true;
        }
    }

    public async Task<Result<IReadOnlyList<TransactionEventDto>>> Handle(
        GetTransactionEventsQuery request, CancellationToken cancellationToken)
    {
        var events = await _events
            .Find(e => e.TransactionId == request.TransactionId && e.UserId == _currentUser.UserId)
            .SortByDescending(e => e.OccurredOnUtc)
            .ToListAsync(cancellationToken);

        var dtos = events.Select(e => new TransactionEventDto(
            e.Id,
            e.TransactionId,
            e.EventType,
            e.OccurredOnUtc,
            e.Summary,
            e.DataJson
        )).ToList();

        return Result<IReadOnlyList<TransactionEventDto>>.Success(dtos);
    }
}
