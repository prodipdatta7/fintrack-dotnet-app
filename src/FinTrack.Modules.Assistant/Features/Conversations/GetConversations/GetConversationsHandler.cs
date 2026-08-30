using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Domain;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.Conversations.GetConversations;

internal sealed class GetConversationsHandler
    : IRequestHandler<GetConversationsQuery, Result<ConversationListResult>>
{
    private readonly IMongoCollection<AssistantConversation> _conversations;
    private readonly IMongoCollection<AssistantMessage> _messages;
    private readonly ICurrentUser _currentUser;
    private static bool _indexCreated;

    public GetConversationsHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _conversations = database.GetCollection<AssistantConversation>("assistant_conversations");
        _messages = database.GetCollection<AssistantMessage>("assistant_messages");
        _currentUser = currentUser;

        if (!_indexCreated)
        {
            var indexKeys = Builders<AssistantConversation>.IndexKeys
                .Ascending(c => c.UserId)
                .Descending(c => c.LastMessageAt);
            _conversations.Indexes.CreateOne(new CreateIndexModel<AssistantConversation>(indexKeys));

            var msgIndex = Builders<AssistantMessage>.IndexKeys
                .Ascending(m => m.UserId)
                .Ascending(m => m.ConversationId)
                .Ascending(m => m.CreatedAt);
            _messages.Indexes.CreateOne(new CreateIndexModel<AssistantMessage>(msgIndex));

            _indexCreated = true;
        }
    }

    public async Task<Result<ConversationListResult>> Handle(
        GetConversationsQuery request, CancellationToken ct)
    {
        var builder = Builders<AssistantConversation>.Filter;
        var filter = builder.Eq(c => c.UserId, _currentUser.UserId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var regex = new BsonRegularExpression(request.SearchTerm.Trim(), "i");
            filter &= builder.Regex(c => c.Title, regex);
        }

        var totalCount = (int)await _conversations.CountDocumentsAsync(filter, cancellationToken: ct);
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 30 : request.PageSize;

        var items = await _conversations.Find(filter)
            .SortByDescending(c => c.IsPinned)
            .ThenByDescending(c => c.LastMessageAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        var convIds = items.Select(c => c.Id).ToList();
        var msgCounts = new Dictionary<string, int>();

        if (convIds.Count > 0)
        {
            var msgFilter = Builders<AssistantMessage>.Filter.Eq(m => m.UserId, _currentUser.UserId) &
                            Builders<AssistantMessage>.Filter.In(m => m.ConversationId, convIds);

            var msgs = await _messages.Find(msgFilter).ToListAsync(ct);
            msgCounts = msgs.GroupBy(m => m.ConversationId).ToDictionary(g => g.Key, g => g.Count());
        }

        var dtos = items.Select(c => new ConversationDto(
            Id: c.Id,
            Title: c.Title,
            IsPinned: c.IsPinned,
            CreatedAt: c.CreatedAt,
            LastMessageAt: c.LastMessageAt,
            MessageCount: msgCounts.GetValueOrDefault(c.Id, 0))).ToList();

        return Result<ConversationListResult>.Success(new ConversationListResult(dtos, totalCount, page, pageSize));
    }
}
