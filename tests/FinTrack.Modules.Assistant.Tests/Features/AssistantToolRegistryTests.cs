using System.Text.Json;
using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Features.GetPortfolioOrAccountBalance;
using FinTrack.Modules.Assistant.Features.ProposeCreateTransaction;
using FinTrack.Modules.Assistant.Services;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features;

public class AssistantToolRegistryTests
{
    [Fact]
    public void GetToolDefinitions_ReturnsAllElevenToolsWithPascalCaseNames()
    {
        var sender = Substitute.For<ISender>();
        var registry = new AssistantToolRegistry(sender);

        var definitions = registry.GetToolDefinitions();

        definitions.Should().HaveCount(11);
        definitions.Select(d => d.Name).Should().BeEquivalentTo([
            "GetPortfolioOrAccountBalance",
            "GetActiveAccountsSummary",
            "GetTopSpendingExpenses",
            "GetCategorySpendingVsBudget",
            "GetSavingsPlansStatus",
            "ProposeCreateTransaction",
            "ProposeCreateAccount",
            "ProposeCreateCategory",
            "ProposeCreateTag",
            "ProposeCreateSavingsPlan",
            "ProposeTransfer"
        ]);
    }

    [Fact]
    public async Task ExecuteToolAsync_DispatchesReadToolToSender()
    {
        var sender = Substitute.For<ISender>();
        var expectedResult = new PortfolioOrAccountBalanceResult(25000m, "BDT", 1, null, []);

        sender.Send(Arg.Any<GetPortfolioOrAccountBalanceQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<PortfolioOrAccountBalanceResult>.Success(expectedResult));

        var registry = new AssistantToolRegistry(sender);
        var args = JsonDocument.Parse("{}").RootElement;

        var result = await registry.ExecuteToolAsync("GetPortfolioOrAccountBalance", args, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedResult);
    }
}
