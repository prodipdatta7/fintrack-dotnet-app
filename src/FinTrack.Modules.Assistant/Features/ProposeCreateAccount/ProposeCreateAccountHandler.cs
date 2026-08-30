using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.ProposeCreateAccount;

internal sealed class ProposeCreateAccountHandler
    : IRequestHandler<ProposeCreateAccountCommand, Result<ProposedActionDto<ProposedCreateAccountPayload>>>
{
    private readonly IMongoCollection<BsonDocument> _accounts;
    private readonly ICurrentUser _currentUser;

    public ProposeCreateAccountHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _accounts = database.GetCollection<BsonDocument>("accounts");
        _currentUser = currentUser;
    }

    public async Task<Result<ProposedActionDto<ProposedCreateAccountPayload>>> Handle(
        ProposeCreateAccountCommand request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        var type = request.Type.Trim();
        var initialBalance = request.InitialBalance ?? 0m;
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "BDT" : request.Currency.Trim();
        var color = string.IsNullOrWhiteSpace(request.Color) ? "#6366f1" : request.Color.Trim();

        var existing = await _accounts.Find(
            Builders<BsonDocument>.Filter.Eq("UserId", _currentUser.UserId) &
            Builders<BsonDocument>.Filter.Regex("Name", new BsonRegularExpression($"^{RegexEscape(name)}$", "i"))
        ).FirstOrDefaultAsync(ct);

        var alreadyExists = existing != null;
        var summary = alreadyExists
            ? $"Account \"{name}\" already exists. Create another {type} account named \"{name}\" with initial balance ৳{initialBalance:N0}?"
            : $"Create a new {type} account named \"{name}\" with an initial balance of ৳{initialBalance:N0} ({currency}).";

        var payload = new ProposedCreateAccountPayload(
            Name: name,
            AccountType: type,
            InitialBalance: initialBalance,
            Currency: currency,
            Color: color,
            AlreadyExists: alreadyExists);

        var proposedAction = ProposedActionDto<ProposedCreateAccountPayload>.Create(
            actionType: "AddAccount",
            summary: summary,
            payload: payload);

        return Result<ProposedActionDto<ProposedCreateAccountPayload>>.Success(proposedAction);
    }

    private static string RegexEscape(string input) =>
        System.Text.RegularExpressions.Regex.Escape(input);
}
