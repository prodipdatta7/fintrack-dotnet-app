using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Pagination;
using FinTrack.Modules.Transactions.Domain;
using MediatR;

namespace FinTrack.Modules.Transactions.Features.GetTransactions;

public sealed record GetTransactionsQuery(
    int Page = 1,
    int PageSize = 10,
    TransactionType? Type = null,
    string? CategoryId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? AccountId = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    string? SearchTerm = null,
    string? SortBy = null) : IRequest<Result<PagedResult<TransactionDto>>>;

public sealed record TransactionAttachmentDto(string FileName, string FileUrl);

public sealed record TransactionDto(
    string Id,
    string Title,
    decimal Amount,
    TransactionType Type,
    string CategoryId,
    string AccountId,
    DateTime Date,
    int TimeZoneOffsetInMinutes = 0,
    string Time = "",
    string PaymentMethod = "",
    string ReceiptFileName = "",
    string ReceiptUrl = "",
    string Tags = "",
    string Note = "",
    IReadOnlyList<TransactionAttachmentDto>? Attachments = null);
