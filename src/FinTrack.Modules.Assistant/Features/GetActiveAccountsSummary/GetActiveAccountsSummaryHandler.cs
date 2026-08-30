using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.GetActiveAccountsSummary;

internal sealed class GetActiveAccountsSummaryHandler
    : IRequestHandler<GetActiveAccountsSummaryQuery, Result<ActiveAccountsSummaryResult>>
{
    private readonly IMongoCollection<BsonDocument> _accounts;
    private readonly ICurrentUser _currentUser;

    public GetActiveAccountsSummaryHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _accounts = database.GetCollection<BsonDocument>("accounts");
        _currentUser = currentUser;
    }

    public async Task<Result<ActiveAccountsSummaryResult>> Handle(
        GetActiveAccountsSummaryQuery request, CancellationToken ct)
    {
        var builder = Builders<BsonDocument>.Filter;
        var filter = builder.Eq("UserId", _currentUser.UserId);

        if (!request.IncludeClosed)
            filter &= builder.Eq("IsClosed", false);

        var docs = await _accounts.Find(filter)
            .Sort(Builders<BsonDocument>.Sort.Ascending("Name"))
            .ToListAsync(ct);

        var accounts = docs.Select(doc => new AccountSummaryItemDto(
            Id: doc.TryGetValue("_id", out var id) ? id.ToString() ?? string.Empty : string.Empty,
            Name: doc.TryGetValue("Name", out var name) ? name.AsString : string.Empty,
            AccountType: doc.TryGetValue("AccountType", out var type) ? type.AsString : string.Empty,
            Balance: doc.TryGetValue("Balance", out var bal) ? bal.ToDecimal() : 0m,
            Currency: doc.TryGetValue("Currency", out var cur) ? cur.AsString : "BDT",
            Color: doc.TryGetValue("Color", out var col) ? col.AsString : "#6366f1",
            IsClosed: doc.TryGetValue("IsClosed", out var closed) && closed.AsBoolean)).ToList();

        var totalBalance = accounts.Where(a => !a.IsClosed).Sum(a => a.Balance);

        var result = new ActiveAccountsSummaryResult(
            TotalPortfolioBalance: totalBalance,
            ActiveAccountCount: accounts.Count(a => !a.IsClosed),
            Accounts: accounts);

        return Result<ActiveAccountsSummaryResult>.Success(result);
    }
}
