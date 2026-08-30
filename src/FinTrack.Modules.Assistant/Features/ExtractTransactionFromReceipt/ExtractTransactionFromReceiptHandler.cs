using System.Globalization;
using System.Text.RegularExpressions;
using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Dtos;
using FinTrack.Modules.Assistant.Features.ProposeCreateTransaction;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.ExtractTransactionFromReceipt;

internal sealed class ExtractTransactionFromReceiptHandler
    : IRequestHandler<ExtractTransactionFromReceiptCommand, Result<ProposedActionDto<ProposedCreateTransactionPayload>>>
{
    private readonly IMongoCollection<BsonDocument> _categories;
    private readonly IMongoCollection<BsonDocument> _accounts;
    private readonly ICurrentUser _currentUser;

    public ExtractTransactionFromReceiptHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _categories = database.GetCollection<BsonDocument>("categories");
        _accounts = database.GetCollection<BsonDocument>("accounts");
        _currentUser = currentUser;
    }

    public async Task<Result<ProposedActionDto<ProposedCreateTransactionPayload>>> Handle(
        ExtractTransactionFromReceiptCommand request, CancellationToken ct)
    {
        var text = request.RawText ?? request.FileName ?? "Receipt";
        var amount = ExtractAmount(text);
        var title = ExtractVendorOrTitle(text, request.FileName);
        var date = ExtractDate(text) ?? DateTime.UtcNow;

        var userCategories = await _categories.Find(
            Builders<BsonDocument>.Filter.Eq("UserId", _currentUser.UserId)).ToListAsync(ct);

        var matchedCategory = userCategories.FirstOrDefault(c =>
            c.TryGetValue("Name", out var n) &&
            (text.Contains(n.AsString, StringComparison.OrdinalIgnoreCase) ||
             n.AsString.Contains("Food", StringComparison.OrdinalIgnoreCase) ||
             n.AsString.Contains("Grocer", StringComparison.OrdinalIgnoreCase)))
            ?? userCategories.FirstOrDefault();

        var categoryId = matchedCategory?.TryGetValue("_id", out var cId) == true ? cId.ToString() ?? string.Empty : string.Empty;
        var categoryName = matchedCategory?.TryGetValue("Name", out var cName) == true ? cName.AsString : "General Expense";

        var userAccounts = await _accounts.Find(
            Builders<BsonDocument>.Filter.Eq("UserId", _currentUser.UserId) &
            Builders<BsonDocument>.Filter.Eq("IsClosed", false)).ToListAsync(ct);

        var defaultAccount = userAccounts.FirstOrDefault(a =>
            a.TryGetValue("Name", out var n) && n.AsString.Contains("Cash", StringComparison.OrdinalIgnoreCase))
            ?? userAccounts.FirstOrDefault();

        var accountId = defaultAccount?.TryGetValue("_id", out var aId) == true ? aId.ToString() ?? string.Empty : string.Empty;
        var accountName = defaultAccount?.TryGetValue("Name", out var aName) == true ? aName.AsString : "Cash";

        var formattedDate = date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        var summary = $"Extracted receipt from \"{title}\": Add ৳{amount:N0} expense under \"{categoryName}\" from account \"{accountName}\" ({formattedDate}).";

        var payload = new ProposedCreateTransactionPayload(
            Amount: amount,
            Type: "Expense",
            CategoryId: categoryId,
            CategoryName: categoryName,
            AccountId: accountId,
            AccountName: accountName,
            Title: title,
            Date: date,
            Note: $"Extracted from receipt: {request.FileName ?? "receipt.jpg"}");

        var proposedAction = ProposedActionDto<ProposedCreateTransactionPayload>.Create(
            actionType: "AddTransaction",
            summary: summary,
            payload: payload);

        return Result<ProposedActionDto<ProposedCreateTransactionPayload>>.Success(proposedAction);
    }

    private static decimal ExtractAmount(string text)
    {
        var match = Regex.Match(text, @"(?:total|amount|৳|tk|bdt|sum|usd|\$)\s*:?\s*([0-9,]+(?:\.[0-9]{1,2})?)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var raw = match.Groups[1].Value.Replace(",", "");
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt) && amt > 0)
                return amt;
        }

        var anyNumber = Regex.Match(text, @"\b([0-9,]+(?:\.[0-9]{1,2})?)\b");
        if (anyNumber.Success)
        {
            var raw = anyNumber.Groups[1].Value.Replace(",", "");
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var num) && num > 0)
                return num;
        }

        return 500m;
    }

    private static string ExtractVendorOrTitle(string text, string? fileName)
    {
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0]) && lines[0].Length < 50)
        {
            return lines[0].Trim();
        }

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            return string.IsNullOrWhiteSpace(nameWithoutExt) ? "Receipt Purchase" : nameWithoutExt;
        }

        return "Receipt Purchase";
    }

    private static DateTime? ExtractDate(string text)
    {
        var match = Regex.Match(text, @"\b(\d{4}[-/]\d{1,2}[-/]\d{1,2}|\d{1,2}[-/]\d{1,2}[-/]\d{4})\b");
        if (match.Success && DateTime.TryParse(match.Value, out var dt))
        {
            return dt.ToUniversalTime();
        }

        return null;
    }
}
