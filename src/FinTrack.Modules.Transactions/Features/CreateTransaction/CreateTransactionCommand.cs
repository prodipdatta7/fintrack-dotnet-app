using FinTrack.BuildingBlocks;
using FinTrack.Modules.Transactions.Domain;
using MediatR;

using FinTrack.Modules.Transactions.Features.GetTransactions;

namespace FinTrack.Modules.Transactions.Features.CreateTransaction;

public sealed class CreateTransactionCommand : IRequest<Result<string>>
{
    public string Title { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public TransactionType Type { get; init; }
    public string CategoryId { get; init; } = string.Empty;
    public string AccountId { get; init; } = string.Empty;
    public DateTime Date { get; init; } = DateTime.UtcNow;
    public int TimeZoneOffsetInMinutes { get; init; }
    public string Time { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string ReceiptFileName { get; init; } = string.Empty;
    public string ReceiptUrl { get; init; } = string.Empty;
    public string Tags { get; init; } = string.Empty;
    public List<TransactionAttachmentDto> Attachments { get; init; } = new();
}
