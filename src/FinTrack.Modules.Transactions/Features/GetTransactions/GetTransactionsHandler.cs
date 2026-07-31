using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.BuildingBlocks.Pagination;
using FinTrack.Modules.Transactions.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Transactions.Features.GetTransactions;

internal sealed class GetTransactionsHandler
    : IRequestHandler<GetTransactionsQuery, Result<PagedResult<TransactionDto>>>
{
    private readonly IMongoCollection<Transaction> _transactions;
    private readonly ICurrentUser _currentUser;

    public GetTransactionsHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _transactions = database.GetCollection<Transaction>("transactions");
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<TransactionDto>>> Handle(
        GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var builder = Builders<Transaction>.Filter;
        var filter = builder.Eq(t => t.UserId, _currentUser.UserId);

        if (request.Type.HasValue)
            filter &= builder.Eq(t => t.Type, request.Type.Value);

        if (!string.IsNullOrWhiteSpace(request.CategoryId))
            filter &= builder.Eq(t => t.CategoryId, request.CategoryId);

        if (request.FromDate.HasValue)
            filter &= builder.Gte(t => t.Date, request.FromDate.Value);

        if (request.ToDate.HasValue)
            filter &= builder.Lte(t => t.Date, request.ToDate.Value);

        var totalCount = (int)await _transactions.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var items = await _transactions.Find(filter)
            .SortByDescending(t => t.Date)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(t => new TransactionDto(
            t.Id,
            t.Title,
            t.Amount,
            t.Type,
            t.CategoryId,
            t.AccountId,
            t.Date,
            t.TimeZoneOffsetInMinutes,
            t.Time,
            t.PaymentMethod,
            t.ReceiptFileName,
            t.ReceiptUrl,
            t.Tags,
            t.Attachments?.Select(a => new TransactionAttachmentDto(a.FileName, a.FileUrl)).ToList() ?? new List<TransactionAttachmentDto>())).ToList();

        var pagedResult = new PagedResult<TransactionDto>(dtos, totalCount, page, pageSize);

        return Result<PagedResult<TransactionDto>>.Success(pagedResult);
    }
}
