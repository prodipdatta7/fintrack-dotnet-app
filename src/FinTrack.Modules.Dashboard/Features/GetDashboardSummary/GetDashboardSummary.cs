using FinTrack.BuildingBlocks;
using FinTrack.Modules.Dashboard.Domain;
using MediatR;
using MongoDB.Entities;

namespace FinTrack.Modules.Dashboard.Features.GetDashboardSummary;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public record DashboardSummaryDto(
    decimal TotalBalance,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetSavings,
    int TransactionCount,
    int AccountCount,
    int CategoryCount,
    int ActiveBudgetCount);

internal sealed class GetDashboardSummaryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly ICurrentUser _currentUser;

    public GetDashboardSummaryHandler(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User must be authenticated.");

        // Get the latest snapshot for this user
        var snapshot = await DB.Find<DashboardSnapshot>()
            .Match(s => s.UserId == userId)
            .Sort(s => s.SnapshotDate, MongoDB.Entities.Order.Descending)
            .ExecuteFirstAsync(cancellationToken);

        if (snapshot is null)
        {
            return new DashboardSummaryDto(0, 0, 0, 0, 0, 0, 0, 0);
        }

        return new DashboardSummaryDto(
            snapshot.TotalBalance,
            snapshot.TotalIncome,
            snapshot.TotalExpense,
            snapshot.NetSavings,
            snapshot.TransactionCount,
            snapshot.AccountCount,
            snapshot.CategoryCount,
            snapshot.ActiveBudgetCount);
    }
}
