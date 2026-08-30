using FinTrack.Modules.Assistant.Features.ProposeCreateSavingsPlan;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features;

public class ProposeCreateSavingsPlanHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsProposedActionWithTargetAmountAndDefaultDeadline()
    {
        var handler = new ProposeCreateSavingsPlanHandler();
        var command = new ProposeCreateSavingsPlanCommand(
            Name: "Home Renovation",
            TargetAmount: 250000m,
            InitialAmount: 20000m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ActionType.Should().Be("CreateSavingsPlan");
        result.Value.Status.Should().Be("Proposed");
        result.Value.Payload.Title.Should().Be("Home Renovation");
        result.Value.Payload.TargetAmount.Should().Be(250000m);
        result.Value.Payload.InitialAmount.Should().Be(20000m);
        result.Value.Payload.Deadline.Should().BeAfter(DateTime.UtcNow);
    }
}
