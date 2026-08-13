using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Dashboard.Features.GetDashboardSummary;

public sealed record GetDashboardSummaryQuery(
    DateTime? From = null,
    DateTime? To = null,
    string? AccountId = null) : IRequest<Result<DashboardSummaryDto>>;

public sealed record CategorySpentDto(string CategoryId, decimal Spent);

public sealed record DashboardAttachmentDto(string FileName, string FileUrl);

/// <summary>
/// Mirrors the wire shape of the transactions API's TransactionDto
/// (FinTrack.Modules.Transactions.Features.GetTransactions). The Dashboard module must not
/// reference the Transactions assembly, so the shape is duplicated here; <c>Type</c> is the
/// raw enum value (1 = Income, 2 = Expense), which serializes to the same JSON number the
/// transactions API emits.
/// </summary>
public sealed record DashboardTransactionDto(
    string Id,
    string Title,
    decimal Amount,
    int Type,
    string CategoryId,
    string AccountId,
    DateTime Date,
    int TimeZoneOffsetInMinutes = 0,
    string Time = "",
    string PaymentMethod = "",
    string ReceiptFileName = "",
    string ReceiptUrl = "",
    string Tags = "",
    IReadOnlyList<DashboardAttachmentDto>? Attachments = null);

public sealed record DashboardSummaryDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetSavings,
    IReadOnlyList<CategorySpentDto> CategorySpent,
    IReadOnlyList<DashboardTransactionDto> RecentTransactions,
    long TransactionCount);
