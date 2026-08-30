using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Domain;
using FinTrack.Modules.Assistant.Dtos;
using FinTrack.Modules.Assistant.Features.GetPortfolioOrAccountBalance;
using FinTrack.Modules.Assistant.Features.ProcessVoiceTurn;
using FinTrack.Modules.Assistant.Features.ProposeCreateTransaction;
using FinTrack.Modules.Assistant.Features.ProposeTransfer;
using FinTrack.Modules.Assistant.Services;
using FinTrack.Modules.Assistant.Services.Ai;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features;

public class ProcessVoiceTurnHandlerTests
{
    private const string TestUserId = "user-voice-123";

    [Fact]
    public async Task Handle_ProcessesBalanceVoiceQuery_AndSavesMessages()
    {
        var convs = new List<AssistantConversation>
        {
            new() { Id = "c-1", UserId = TestUserId, Title = "Voice Chat" }
        };
        var msgs = new List<AssistantMessage>();
        var db = AssistantTestHelpers.MockConversationDatabase(convs, msgs);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetPortfolioOrAccountBalanceQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<PortfolioOrAccountBalanceResult>.Success(
                new PortfolioOrAccountBalanceResult(50000m, "BDT", 3, null, new List<AccountBalanceItemDto>())));

        var guardrails = new AssistantGuardrailsService();
        var gemini = Substitute.For<IGeminiAiService>();
        gemini.IsConfigured.Returns(false); // Fast-path fallback
        var toolRegistry = new AssistantToolRegistry(sender);

        var handler = new ProcessVoiceTurnHandler(db, currentUser, sender, guardrails, gemini, toolRegistry);
        var cmd = new ProcessVoiceTurnCommand("c-1", "What is my total balance?");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AssistantReply.Should().Contain("৳50,000.00");
        result.Value.ToolName.Should().Be("GetPortfolioOrAccountBalance");

        // Both user voice transcript and assistant reply are saved
        msgs.Should().HaveCount(2);
        msgs[0].Role.Should().Be("user");
        msgs[0].Content.Should().Be("What is my total balance?");
        msgs[1].Role.Should().Be("assistant");
        msgs[1].Content.Should().Contain("50,000.00");
    }

    [Fact]
    public async Task Handle_ProcessesAddTransactionVoiceQuery_AndReturnsProposedCard()
    {
        var convs = new List<AssistantConversation>
        {
            new() { Id = "c-1", UserId = TestUserId, Title = "Voice Chat" }
        };
        var msgs = new List<AssistantMessage>();
        var db = AssistantTestHelpers.MockConversationDatabase(convs, msgs);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ProposeCreateTransactionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProposedActionDto<ProposedCreateTransactionPayload>>.Success(
                new ProposedActionDto<ProposedCreateTransactionPayload>(
                    ActionType: "AddTransaction",
                    Status: "Proposed",
                    Summary: "Add ৳350 Expense under Food & Dining",
                    Payload: new ProposedCreateTransactionPayload(
                        Amount: 350m,
                        Type: "Expense",
                        CategoryId: "cat-1",
                        CategoryName: "Food & Dining",
                        AccountId: "acc-1",
                        AccountName: "Cash",
                        Title: "Food & Dining",
                        Date: DateTime.UtcNow,
                        Note: "Lunch"
                    )
                )));

        var guardrails = new AssistantGuardrailsService();
        var gemini = Substitute.For<IGeminiAiService>();
        gemini.IsConfigured.Returns(false);
        var toolRegistry = new AssistantToolRegistry(sender);

        var handler = new ProcessVoiceTurnHandler(db, currentUser, sender, guardrails, gemini, toolRegistry);
        var cmd = new ProcessVoiceTurnCommand("c-1", "Add 350 taka for lunch");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ActionType.Should().Be("AddTransaction");
        result.Value.ActionStatus.Should().Be("Proposed");
        result.Value.AssistantReply.Should().Contain("Add ৳350 Expense");

        msgs.Should().HaveCount(2);
        msgs[1].ActionType.Should().Be("AddTransaction");
        msgs[1].ActionStatus.Should().Be("Proposed");
    }

    [Fact]
    public async Task Handle_UpdatesExistingProposedTransaction_WhenFollowUpModifiesAccount()
    {
        var convs = new List<AssistantConversation>
        {
            new() { Id = "c-1", UserId = TestUserId, Title = "Voice Chat" }
        };
        var msgs = new List<AssistantMessage>
        {
            new()
            {
                Id = "msg-prev-proposed",
                ConversationId = "c-1",
                UserId = TestUserId,
                Role = "assistant",
                Content = "I have prepared a transaction: Add ৳55 Expense under Food & Dining from Default Account",
                ActionType = "AddTransaction",
                ActionStatus = "Proposed",
                ActionSummary = "Add ৳55 Expense under Food & Dining",
                ActionPayloadJson = "{\"Amount\":55,\"CategoryName\":\"Food & Dining\",\"AccountName\":\"Default Account\",\"Type\":\"Expense\",\"Title\":\"Food & Dining\"}",
                CreatedAt = DateTime.UtcNow.AddSeconds(-10)
            }
        };
        var db = AssistantTestHelpers.MockConversationDatabase(convs, msgs);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Is<ProposeCreateTransactionCommand>(c => c.Account == "Cash" && c.Amount == 55), Arg.Any<CancellationToken>())
            .Returns(Result<ProposedActionDto<ProposedCreateTransactionPayload>>.Success(
                new ProposedActionDto<ProposedCreateTransactionPayload>(
                    ActionType: "AddTransaction",
                    Status: "Proposed",
                    Summary: "Add ৳55 Expense under Food & Dining from account Cash",
                    Payload: new ProposedCreateTransactionPayload(
                        Amount: 55m,
                        Type: "Expense",
                        CategoryId: "cat-1",
                        CategoryName: "Food & Dining",
                        AccountId: "acc-cash",
                        AccountName: "Cash",
                        Title: "Food & Dining",
                        Date: DateTime.UtcNow,
                        Note: "it would be from Cash"
                    )
                )));

        var guardrails = new AssistantGuardrailsService();
        var gemini = Substitute.For<IGeminiAiService>();
        gemini.IsConfigured.Returns(false);
        var toolRegistry = new AssistantToolRegistry(sender);

        var handler = new ProcessVoiceTurnHandler(db, currentUser, sender, guardrails, gemini, toolRegistry);
        var cmd = new ProcessVoiceTurnCommand("c-1", "it would be from Cash");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ActionType.Should().Be("AddTransaction");
        result.Value.ActionStatus.Should().Be("Proposed");
        result.Value.AssistantReply.Should().Contain("from account Cash");
    }

    [Fact]
    public async Task Handle_DispatchesGeminiFunctionCalling_WhenGeminiIsConfigured()
    {
        var convs = new List<AssistantConversation>
        {
            new() { Id = "c-1", UserId = TestUserId, Title = "Voice Chat" }
        };
        var msgs = new List<AssistantMessage>();
        var db = AssistantTestHelpers.MockConversationDatabase(convs, msgs);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ProposeCreateTransactionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProposedActionDto<ProposedCreateTransactionPayload>>.Success(
                new ProposedActionDto<ProposedCreateTransactionPayload>(
                    ActionType: "AddTransaction",
                    Status: "Proposed",
                    Summary: "Add ৳1200 Expense under Food & Dining from account City Bank",
                    Payload: new ProposedCreateTransactionPayload(
                        Amount: 1200m,
                        Type: "Expense",
                        CategoryId: "cat-1",
                        CategoryName: "Food & Dining",
                        AccountId: "acc-city",
                        AccountName: "City Bank",
                        Title: "Team Dinner",
                        Date: DateTime.UtcNow,
                        Note: "Team dinner"
                    )
                )));

        var guardrails = new AssistantGuardrailsService();
        var gemini = Substitute.For<IGeminiAiService>();
        gemini.IsConfigured.Returns(true);
        gemini.ProcessTurnAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<AssistantMessage>>(), Arg.Any<IReadOnlyList<ToolDefinitionDto>>(), Arg.Any<CancellationToken>())
            .Returns(new GeminiTurnResult(
                IsSuccess: true,
                FunctionCallName: "ProposeCreateTransaction",
                FunctionCallArgsJson: "{\"amount\":1200,\"category\":\"Food & Dining\",\"account\":\"City Bank\",\"title\":\"Team Dinner\"}"));

        var toolRegistry = new AssistantToolRegistry(sender);
        var handler = new ProcessVoiceTurnHandler(db, currentUser, sender, guardrails, gemini, toolRegistry);
        var cmd = new ProcessVoiceTurnCommand("c-1", "I spent 1200 on team dinner from city bank");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ActionType.Should().Be("AddTransaction");
        result.Value.ActionStatus.Should().Be("Proposed");
        result.Value.ToolName.Should().Be("ProposeCreateTransaction");
        result.Value.AssistantReply.Should().Contain("1200");
    }

    [Fact]
    public async Task Handle_ProcessesIncomeVoiceQuery_AndReturnsIncomeProposedCard()
    {
        var convs = new List<AssistantConversation>
        {
            new() { Id = "c-1", UserId = TestUserId, Title = "Voice Chat" }
        };
        var msgs = new List<AssistantMessage>();
        var db = AssistantTestHelpers.MockConversationDatabase(convs, msgs);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Is<ProposeCreateTransactionCommand>(c => c.Type == "Income" && c.Amount == 50000m), Arg.Any<CancellationToken>())
            .Returns(Result<ProposedActionDto<ProposedCreateTransactionPayload>>.Success(
                new ProposedActionDto<ProposedCreateTransactionPayload>(
                    ActionType: "AddTransaction",
                    Status: "Proposed",
                    Summary: "Add ৳50,000 income for \"Salary\" under category \"Salary\" to account \"Bank Account\"",
                    Payload: new ProposedCreateTransactionPayload(
                        Amount: 50000m,
                        Type: "Income",
                        CategoryId: "cat-sal",
                        CategoryName: "Salary",
                        AccountId: "acc-bank",
                        AccountName: "Bank Account",
                        Title: "Salary",
                        Date: DateTime.UtcNow,
                        Note: "received salary of 50000 into Bank Account"
                    )
                )));

        var guardrails = new AssistantGuardrailsService();
        var gemini = Substitute.For<IGeminiAiService>();
        gemini.IsConfigured.Returns(false);
        var toolRegistry = new AssistantToolRegistry(sender);

        var handler = new ProcessVoiceTurnHandler(db, currentUser, sender, guardrails, gemini, toolRegistry);
        var cmd = new ProcessVoiceTurnCommand("c-1", "received salary of 50000 into Bank Account");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ActionType.Should().Be("AddTransaction");
        result.Value.ActionStatus.Should().Be("Proposed");
        result.Value.ActionPayloadJson.Should().Contain("Income");
        result.Value.AssistantReply.Should().Contain("50,000");
    }

    [Fact]
    public async Task Handle_ProcessesTransferVoiceQuery_AndReturnsTransferProposedCard()
    {
        var convs = new List<AssistantConversation>
        {
            new() { Id = "c-1", UserId = TestUserId, Title = "Voice Chat" }
        };
        var msgs = new List<AssistantMessage>();
        var db = AssistantTestHelpers.MockConversationDatabase(convs, msgs);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Is<ProposeTransferCommand>(c => c.Amount == 3000m && c.FromAccount == "Bank Account" && c.ToAccount == "Cash"), Arg.Any<CancellationToken>())
            .Returns(Result<ProposedActionDto<ProposedTransferPayload>>.Success(
                new ProposedActionDto<ProposedTransferPayload>(
                    ActionType: "TransferFunds",
                    Status: "Proposed",
                    Summary: "Transfer ৳3,000 from account \"Bank Account\" to \"Cash\"",
                    Payload: new ProposedTransferPayload(
                        Amount: 3000m,
                        FromAccountId: "acc-bank",
                        FromAccountName: "Bank Account",
                        ToAccountId: "acc-cash",
                        ToAccountName: "Cash",
                        Date: DateTime.UtcNow,
                        Note: "transfer 3000 from Bank Account to Cash"
                    )
                )));

        var guardrails = new AssistantGuardrailsService();
        var gemini = Substitute.For<IGeminiAiService>();
        gemini.IsConfigured.Returns(false);
        var toolRegistry = new AssistantToolRegistry(sender);

        var handler = new ProcessVoiceTurnHandler(db, currentUser, sender, guardrails, gemini, toolRegistry);
        var cmd = new ProcessVoiceTurnCommand("c-1", "transfer 3000 from Bank Account to Cash");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ActionType.Should().Be("TransferFunds");
        result.Value.ActionStatus.Should().Be("Proposed");
        result.Value.ToolName.Should().Be("ProposeTransfer");
        result.Value.AssistantReply.Should().Contain("transfer");
    }

    [Fact]
    public async Task Handle_ReturnsError_WhenConversationNotFound()
    {
        var convs = new List<AssistantConversation>();
        var msgs = new List<AssistantMessage>();
        var db = AssistantTestHelpers.MockConversationDatabase(convs, msgs);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var sender = Substitute.For<ISender>();
        var guardrails = new AssistantGuardrailsService();
        var gemini = Substitute.For<IGeminiAiService>();
        var toolRegistry = new AssistantToolRegistry(sender);

        var handler = new ProcessVoiceTurnHandler(db, currentUser, sender, guardrails, gemini, toolRegistry);
        var cmd = new ProcessVoiceTurnCommand("c-999", "Hello");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }
}
