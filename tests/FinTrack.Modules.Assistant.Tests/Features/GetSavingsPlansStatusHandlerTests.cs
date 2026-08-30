using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Features.GetSavingsPlansStatus;
using FluentAssertions;
using MongoDB.Bson;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features;

public class GetSavingsPlansStatusHandlerTests
{
    private const string TestUserId = "user-test-123";

    [Fact]
    public async Task Handle_ReturnsSavingsPlansWithProgressAndDeadlines()
    {
        var plans = new List<BsonDocument>
        {
            new() { { "_id", "plan-1" }, { "UserId", TestUserId }, { "Title", "New Laptop" }, { "TargetAmount", 100000m }, { "CurrentAmount", 40000m }, { "Color", "#6366f1" }, { "Deadline", DateTime.UtcNow.AddMonths(3) } },
            new() { { "_id", "plan-2" }, { "UserId", TestUserId }, { "Title", "Emergency Fund" }, { "TargetAmount", 50000m }, { "CurrentAmount", 50000m }, { "Color", "#10b981" }, { "Deadline", DateTime.UtcNow.AddMonths(1) } }
        };

        var db = AssistantTestHelpers.MockDatabase(new() { ["savings_plans"] = plans });
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new GetSavingsPlansStatusHandler(db, currentUser);
        var result = await handler.Handle(new GetSavingsPlansStatusQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PlanCount.Should().Be(2);
        result.Value.TotalTargetAmount.Should().Be(150000m);
        result.Value.TotalCurrentAmount.Should().Be(90000m);
        result.Value.TotalRemainingAmount.Should().Be(60000m);
        result.Value.Plans.First(p => p.PlanId == "plan-1").ProgressPercentage.Should().Be(40m);
        result.Value.Plans.First(p => p.PlanId == "plan-2").IsCompleted.Should().BeTrue();
    }
}
