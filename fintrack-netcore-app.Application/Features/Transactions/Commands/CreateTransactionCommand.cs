using fintrack_netcore_app.Domain.Enums;
using MediatR;
namespace fintrack_netcore_app.Application.Features.Transactions.Commands
{
    public record CreateTransactionCommand(
        string Title,
        decimal Amount,
        TransactionType Type,
        int TimeZoneOffsetInMinutes) : IRequest<string>;
}
