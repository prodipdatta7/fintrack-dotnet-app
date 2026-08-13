using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Transactions.Domain;
using FinTrack.Modules.Transactions.Features.GetTransactions;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Transactions.Features.GetTransaction;

internal sealed class GetTransactionHandler : IRequestHandler<GetTransactionQuery, Result<TransactionDto>>
{
    private readonly IMongoCollection<Transaction> _transactions;
    private readonly ICurrentUser _currentUser;

    public GetTransactionHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _transactions = database.GetCollection<Transaction>("transactions");
        _currentUser = currentUser;
    }

    public async Task<Result<TransactionDto>> Handle(
        GetTransactionQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _transactions
            .Find(t => t.Id == request.Id && t.UserId == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (transaction is null)
            return Result<TransactionDto>.Failure("Transaction not found.");

        var dto = new TransactionDto(
            transaction.Id,
            transaction.Title,
            transaction.Amount,
            transaction.Type,
            transaction.CategoryId,
            transaction.AccountId,
            transaction.Date,
            transaction.TimeZoneOffsetInMinutes,
            transaction.Time,
            transaction.PaymentMethod,
            transaction.ReceiptFileName,
            transaction.ReceiptUrl,
            transaction.Tags,
            transaction.Note ?? string.Empty,
            transaction.Attachments?.Select(a => new TransactionAttachmentDto(a.FileName, a.FileUrl)).ToList() ?? new List<TransactionAttachmentDto>());

        return Result<TransactionDto>.Success(dto);
    }
}
