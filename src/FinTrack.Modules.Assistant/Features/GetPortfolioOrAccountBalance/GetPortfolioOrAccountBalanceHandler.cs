using System.Text.RegularExpressions;
using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.GetPortfolioOrAccountBalance;

internal sealed class GetPortfolioOrAccountBalanceHandler
    : IRequestHandler<GetPortfolioOrAccountBalanceQuery, Result<PortfolioOrAccountBalanceResult>>
{
    private readonly IMongoCollection<BsonDocument> _accounts;
    private readonly ICurrentUser _currentUser;

    public GetPortfolioOrAccountBalanceHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _accounts = database.GetCollection<BsonDocument>("accounts");
        _currentUser = currentUser;
    }

    public async Task<Result<PortfolioOrAccountBalanceResult>> Handle(
        GetPortfolioOrAccountBalanceQuery request, CancellationToken ct)
    {
        var builder = Builders<BsonDocument>.Filter;
        var baseFilter = builder.Eq("UserId", _currentUser.UserId) & builder.Eq("IsClosed", false);

        var allAccountsDocs = await _accounts.Find(baseFilter).ToListAsync(ct);

        var allAccountItems = allAccountsDocs.Select(doc => new AccountBalanceItemDto(
            Id: doc.TryGetValue("_id", out var id) ? id.ToString() ?? string.Empty : string.Empty,
            Name: doc.TryGetValue("Name", out var name) ? name.AsString : string.Empty,
            AccountType: doc.TryGetValue("AccountType", out var type) ? type.AsString : string.Empty,
            Balance: doc.TryGetValue("Balance", out var bal) ? bal.ToDecimal() : 0m,
            Currency: doc.TryGetValue("Currency", out var cur) ? cur.AsString : "BDT")).ToList();

        AccountBalanceItemDto? targetAccount = null;

        if (!string.IsNullOrWhiteSpace(request.AccountId))
        {
            targetAccount = allAccountItems.FirstOrDefault(a =>
                string.Equals(a.Id, request.AccountId.Trim(), StringComparison.OrdinalIgnoreCase));
        }
        else if (!string.IsNullOrWhiteSpace(request.AccountName))
        {
            var search = request.AccountName.Trim();
            targetAccount = allAccountItems.FirstOrDefault(a =>
                string.Equals(a.Name, search, StringComparison.OrdinalIgnoreCase))
                ?? allAccountItems.FirstOrDefault(a =>
                a.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var totalBalance = allAccountItems.Sum(a => a.Balance);
        var currency = allAccountItems.FirstOrDefault()?.Currency ?? "BDT";

        var result = new PortfolioOrAccountBalanceResult(
            TotalBalance: targetAccount != null ? targetAccount.Balance : totalBalance,
            Currency: targetAccount?.Currency ?? currency,
            AccountCount: allAccountItems.Count,
            TargetAccount: targetAccount,
            Accounts: allAccountItems);

        return Result<PortfolioOrAccountBalanceResult>.Success(result);
    }
}
