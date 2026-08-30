using System.Globalization;
using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.ProposeCreateTransaction;

internal sealed class ProposeCreateTransactionHandler
    : IRequestHandler<ProposeCreateTransactionCommand, Result<ProposedActionDto<ProposedCreateTransactionPayload>>>
{
    private readonly IMongoCollection<BsonDocument> _categories;
    private readonly IMongoCollection<BsonDocument> _accounts;
    private readonly ICurrentUser _currentUser;

    public ProposeCreateTransactionHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _categories = database.GetCollection<BsonDocument>("categories");
        _accounts = database.GetCollection<BsonDocument>("accounts");
        _currentUser = currentUser;
    }

    public async Task<Result<ProposedActionDto<ProposedCreateTransactionPayload>>> Handle(
        ProposeCreateTransactionCommand request, CancellationToken ct)
    {
        var isIncome = string.Equals(request.Type?.Trim(), "Income", StringComparison.OrdinalIgnoreCase);
        var txType = isIncome ? "Income" : "Expense";

        // 1. Resolve Category
        var userCategories = await _categories.Find(
            Builders<BsonDocument>.Filter.Eq("UserId", _currentUser.UserId)).ToListAsync(ct);

        var requestedCat = request.Category?.Trim() ?? string.Empty;
        if (isIncome && (string.IsNullOrWhiteSpace(requestedCat) || requestedCat.Equals("General Expense", StringComparison.OrdinalIgnoreCase)))
        {
            requestedCat = "Salary";
        }

        var matchedCategory = userCategories.FirstOrDefault(c =>
            string.Equals(c.TryGetValue("_id", out var id) ? id.ToString() : null, requestedCat, StringComparison.OrdinalIgnoreCase))
            ?? userCategories.FirstOrDefault(c =>
            string.Equals(c.TryGetValue("Name", out var n) ? n.AsString : null, requestedCat, StringComparison.OrdinalIgnoreCase))
            ?? userCategories.FirstOrDefault(c =>
            c.TryGetValue("Name", out var n) && n.AsString.Contains(requestedCat, StringComparison.OrdinalIgnoreCase))
            ?? (isIncome ? userCategories.FirstOrDefault(c => c.TryGetValue("Type", out var t) && (t.AsInt32 == 1 || t.AsString == "Income")) : null);

        var categoryId = matchedCategory?.TryGetValue("_id", out var mCatId) == true
            ? mCatId.ToString() ?? string.Empty
            : string.Empty;
        var categoryName = matchedCategory?.TryGetValue("Name", out var mCatName) == true
            ? mCatName.AsString
            : (string.IsNullOrWhiteSpace(requestedCat) ? (isIncome ? "Salary" : "General Expense") : requestedCat);

        // 2. Resolve Account
        var userAccounts = await _accounts.Find(
            Builders<BsonDocument>.Filter.Eq("UserId", _currentUser.UserId) &
            Builders<BsonDocument>.Filter.Eq("IsClosed", false)).ToListAsync(ct);

        var matchedAccount = userAccounts.FirstOrDefault(a =>
            string.Equals(a.TryGetValue("_id", out var id) ? id.ToString() : null, request.Account.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? userAccounts.FirstOrDefault(a =>
            string.Equals(a.TryGetValue("Name", out var n) ? n.AsString : null, request.Account.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? userAccounts.FirstOrDefault(a =>
            a.TryGetValue("Name", out var n) && n.AsString.Contains(request.Account.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? userAccounts.FirstOrDefault();

        var accountId = matchedAccount?.TryGetValue("_id", out var mAccId) == true
            ? mAccId.ToString() ?? string.Empty
            : string.Empty;
        var accountName = matchedAccount?.TryGetValue("Name", out var mAccName) == true
            ? mAccName.AsString
            : request.Account.Trim();

        // 3. Normalize transaction attributes
        var date = request.Date.HasValue ? request.Date.Value.ToUniversalTime() : DateTime.UtcNow;
        var formattedDate = date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        var title = !string.IsNullOrWhiteSpace(request.Title)
            ? request.Title.Trim()
            : categoryName;

        var note = request.Note?.Trim() ?? string.Empty;

        var summary = isIncome
            ? $"Add ৳{request.Amount:N0} income for \"{title}\" under category \"{categoryName}\" to account \"{accountName}\" on {formattedDate}."
            : $"Add ৳{request.Amount:N0} expense for \"{title}\" under category \"{categoryName}\" from account \"{accountName}\" on {formattedDate}.";

        var payload = new ProposedCreateTransactionPayload(
            Amount: request.Amount,
            Type: txType,
            CategoryId: categoryId,
            CategoryName: categoryName,
            AccountId: accountId,
            AccountName: accountName,
            Title: title,
            Date: date,
            Note: note);

        var proposedAction = ProposedActionDto<ProposedCreateTransactionPayload>.Create(
            actionType: "AddTransaction",
            summary: summary,
            payload: payload);

        return Result<ProposedActionDto<ProposedCreateTransactionPayload>>.Success(proposedAction);
    }
}
