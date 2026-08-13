using FinTrack.Modules.Accounts.Features.ApplyAccountBalanceDelta;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Accounts.Tests.Features.ApplyAccountBalanceDelta;

public class ApplyAccountBalanceDeltaHandlerTests
{
    [Theory]
    [InlineData("Income", 100, 100)]
    [InlineData("income", 50.5, 50.5)]
    [InlineData("Expense", 100, -100)]
    [InlineData("expense", 25, -25)]
    public void SignedDelta_MapsIncomeAndExpense(string type, decimal amount, decimal expected)
    {
        ApplyAccountBalanceDeltaHandler.SignedDelta(amount, type).Should().Be(expected);
    }
}
