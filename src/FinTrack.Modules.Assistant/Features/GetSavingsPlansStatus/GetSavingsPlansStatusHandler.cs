using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.GetSavingsPlansStatus;

internal sealed class GetSavingsPlansStatusHandler
    : IRequestHandler<GetSavingsPlansStatusQuery, Result<SavingsPlansStatusResult>>
{
    private readonly IMongoCollection<BsonDocument> _plans;
    private readonly ICurrentUser _currentUser;

    public GetSavingsPlansStatusHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _plans = database.GetCollection<BsonDocument>("savings_plans");
        _currentUser = currentUser;
    }

    public async Task<Result<SavingsPlansStatusResult>> Handle(
        GetSavingsPlansStatusQuery request, CancellationToken ct)
    {
        var builder = Builders<BsonDocument>.Filter;
        var filter = builder.Eq("UserId", _currentUser.UserId);

        var allPlansDocs = await _plans.Find(filter)
            .Sort(Builders<BsonDocument>.Sort.Ascending("Deadline"))
            .ToListAsync(ct);

        var now = DateTime.UtcNow;

        var planItems = allPlansDocs.Select(doc =>
        {
            var planId = doc.TryGetValue("_id", out var id) ? id.ToString() ?? string.Empty : string.Empty;
            var title = doc.TryGetValue("Title", out var t) ? t.AsString : string.Empty;
            var target = doc.TryGetValue("TargetAmount", out var ta) ? ta.ToDecimal() : 0m;
            var current = doc.TryGetValue("CurrentAmount", out var ca) ? ca.ToDecimal() : 0m;
            var color = doc.TryGetValue("Color", out var col) ? col.AsString : "#6366f1";
            var deadline = doc.TryGetValue("Deadline", out var dl) && dl.IsValidDateTime
                ? dl.ToUniversalTime()
                : now.AddMonths(1);

            var remaining = Math.Max(0m, target - current);
            var progress = target > 0m ? Math.Min(100m, Math.Round((current / target) * 100m, 1)) : 0m;
            var daysRemaining = Math.Max(0, (int)(deadline.Date - now.Date).TotalDays);
            var isCompleted = current >= target;

            return new SavingsPlanStatusItemDto(
                PlanId: planId,
                Title: title,
                TargetAmount: target,
                CurrentAmount: current,
                RemainingAmount: remaining,
                ProgressPercentage: progress,
                Color: color,
                Deadline: deadline,
                DaysRemaining: daysRemaining,
                IsCompleted: isCompleted);
        }).ToList();

        if (!string.IsNullOrWhiteSpace(request.PlanId))
        {
            planItems = planItems.Where(p =>
                string.Equals(p.PlanId, request.PlanId.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        }
        else if (!string.IsNullOrWhiteSpace(request.PlanName))
        {
            var search = request.PlanName.Trim();
            var matched = planItems.FirstOrDefault(p =>
                string.Equals(p.Title, search, StringComparison.OrdinalIgnoreCase))
                ?? planItems.FirstOrDefault(p =>
                p.Title.Contains(search, StringComparison.OrdinalIgnoreCase));

            if (matched != null)
                planItems = [matched];
        }

        var totalTarget = planItems.Sum(p => p.TargetAmount);
        var totalCurrent = planItems.Sum(p => p.CurrentAmount);
        var totalRemaining = Math.Max(0m, totalTarget - totalCurrent);
        var overallProgress = totalTarget > 0m
            ? Math.Min(100m, Math.Round((totalCurrent / totalTarget) * 100m, 1))
            : 0m;

        var result = new SavingsPlansStatusResult(
            TotalTargetAmount: totalTarget,
            TotalCurrentAmount: totalCurrent,
            TotalRemainingAmount: totalRemaining,
            OverallProgressPercentage: overallProgress,
            PlanCount: planItems.Count,
            Plans: planItems);

        return Result<SavingsPlansStatusResult>.Success(result);
    }
}
