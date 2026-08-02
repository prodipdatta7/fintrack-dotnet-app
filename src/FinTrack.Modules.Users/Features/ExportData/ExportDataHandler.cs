using System.Text;
using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Users.Features.ExportData;

internal sealed class ExportDataHandler : IRequestHandler<ExportDataCommand, Result<ExportDataResponse>>
{
    private readonly IMongoCollection<BsonDocument> _transactions;
    private readonly ICurrentUser _currentUser;

    public ExportDataHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _transactions = database.GetCollection<BsonDocument>("transactions");
        _currentUser = currentUser;
    }

    public async Task<Result<ExportDataResponse>> Handle(
        ExportDataCommand request, CancellationToken cancellationToken)
    {
        var builder = Builders<BsonDocument>.Filter;
        var filter = builder.Eq("UserId", _currentUser.UserId);

        if (request.FromDate.HasValue)
            filter &= builder.Gte("Date", request.FromDate.Value);

        if (request.ToDate.HasValue)
            filter &= builder.Lte("Date", request.ToDate.Value);

        var transactions = await _transactions
            .Find(filter)
            .Sort(Builders<BsonDocument>.Sort.Descending("Date"))
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Id,Title,Amount,Type,CategoryId,AccountId,Date,PaymentMethod,Tags");

        foreach (var doc in transactions)
        {
            var id = doc.Contains("_id") ? doc["_id"].ToString() : "";
            var title = doc.Contains("Title") ? EscapeCsv(doc["Title"].AsString) : "";
            var amount = doc.Contains("Amount") ? doc["Amount"].ToString() : "0";
            var type = doc.Contains("Type") ? doc["Type"].ToString() : "";
            var categoryId = doc.Contains("CategoryId") ? doc["CategoryId"].AsString : "";
            var accountId = doc.Contains("AccountId") ? doc["AccountId"].AsString : "";
            var date = doc.Contains("Date") ? doc["Date"].ToUniversalTime().ToString("o") : "";
            var paymentMethod = doc.Contains("PaymentMethod") ? EscapeCsv(doc["PaymentMethod"].AsString) : "";
            var tags = doc.Contains("Tags") ? EscapeCsv(doc["Tags"].AsString) : "";

            sb.AppendLine($"{id},{title},{amount},{type},{categoryId},{accountId},{date},{paymentMethod},{tags}");
        }

        var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"fintrack_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        return Result<ExportDataResponse>.Success(
            new ExportDataResponse(csvBytes, "text/csv", fileName));
    }

    private static string EscapeCsv(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
