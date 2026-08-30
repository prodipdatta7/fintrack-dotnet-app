using System.Globalization;
using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.ProposeTransfer;

internal sealed class ProposeTransferHandler
    : IRequestHandler<ProposeTransferCommand, Result<ProposedActionDto<ProposedTransferPayload>>>
{
    private readonly IMongoCollection<BsonDocument> _accounts;
    private readonly ICurrentUser _currentUser;

    public ProposeTransferHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _accounts = database.GetCollection<BsonDocument>("accounts");
        _currentUser = currentUser;
    }

    public async Task<Result<ProposedActionDto<ProposedTransferPayload>>> Handle(
        ProposeTransferCommand request, CancellationToken ct)
    {
        if (request.Amount <= 0)
        {
            return Result<ProposedActionDto<ProposedTransferPayload>>.Failure("Transfer amount must be greater than zero.");
        }

        var userAccounts = await _accounts.Find(
            Builders<BsonDocument>.Filter.Eq("UserId", _currentUser.UserId) &
            Builders<BsonDocument>.Filter.Eq("IsClosed", false)).ToListAsync(ct);

        if (userAccounts.Count == 0)
        {
            return Result<ProposedActionDto<ProposedTransferPayload>>.Failure("No active accounts found for transfer.");
        }

        // 1. Match Source Account (FromAccount)
        var matchedFrom = userAccounts.FirstOrDefault(a =>
            string.Equals(a.TryGetValue("_id", out var id) ? id.ToString() : null, request.FromAccount.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? userAccounts.FirstOrDefault(a =>
            string.Equals(a.TryGetValue("Name", out var n) ? n.AsString : null, request.FromAccount.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? userAccounts.FirstOrDefault(a =>
            a.TryGetValue("Name", out var n) && n.AsString.Contains(request.FromAccount.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? userAccounts.FirstOrDefault();

        // 2. Match Destination Account (ToAccount)
        var matchedTo = userAccounts.FirstOrDefault(a =>
            a != matchedFrom &&
            (string.Equals(a.TryGetValue("_id", out var id) ? id.ToString() : null, request.ToAccount.Trim(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(a.TryGetValue("Name", out var n) ? n.AsString : null, request.ToAccount.Trim(), StringComparison.OrdinalIgnoreCase)
            || (a.TryGetValue("Name", out var name) && name.AsString.Contains(request.ToAccount.Trim(), StringComparison.OrdinalIgnoreCase))))
            ?? userAccounts.FirstOrDefault(a => a != matchedFrom);

        var fromId = matchedFrom?.TryGetValue("_id", out var fId) == true ? fId.ToString() ?? string.Empty : string.Empty;
        var fromName = matchedFrom?.TryGetValue("Name", out var fName) == true ? fName.AsString : request.FromAccount.Trim();

        var toId = matchedTo?.TryGetValue("_id", out var tId) == true ? tId.ToString() ?? string.Empty : string.Empty;
        var toName = matchedTo?.TryGetValue("Name", out var tName) == true ? tName.AsString : request.ToAccount.Trim();

        var date = request.Date.HasValue ? request.Date.Value.ToUniversalTime() : DateTime.UtcNow;
        var formattedDate = date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

        var summary = $"Transfer ৳{request.Amount:N0} from account \"{fromName}\" to \"{toName}\" on {formattedDate}.";

        var payload = new ProposedTransferPayload(
            Amount: request.Amount,
            FromAccountId: fromId,
            FromAccountName: fromName,
            ToAccountId: toId,
            ToAccountName: toName,
            Date: date,
            Note: request.Note?.Trim());

        var proposedAction = ProposedActionDto<ProposedTransferPayload>.Create(
            actionType: "TransferFunds",
            summary: summary,
            payload: payload);

        return Result<ProposedActionDto<ProposedTransferPayload>>.Success(proposedAction);
    }
}
