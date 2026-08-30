using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Domain;
using FinTrack.Modules.Assistant.Dtos;
using FinTrack.Modules.Assistant.Features.GetActiveAccountsSummary;
using FinTrack.Modules.Assistant.Features.GetCategorySpendingVsBudget;
using FinTrack.Modules.Assistant.Features.GetPortfolioOrAccountBalance;
using FinTrack.Modules.Assistant.Features.GetSavingsPlansStatus;
using FinTrack.Modules.Assistant.Features.GetTopSpendingExpenses;
using FinTrack.Modules.Assistant.Features.ProposeCreateAccount;
using FinTrack.Modules.Assistant.Features.ProposeCreateCategory;
using FinTrack.Modules.Assistant.Features.ProposeCreateSavingsPlan;
using FinTrack.Modules.Assistant.Features.ProposeCreateTag;
using FinTrack.Modules.Assistant.Features.ProposeCreateTransaction;
using FinTrack.Modules.Assistant.Features.ProposeTransfer;
using FinTrack.Modules.Assistant.Services;
using FinTrack.Modules.Assistant.Services.Ai;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.ProcessVoiceTurn;

internal sealed class ProcessVoiceTurnHandler
    : IRequestHandler<ProcessVoiceTurnCommand, Result<VoiceTurnResult>>
{
    private readonly IMongoCollection<AssistantConversation> _conversations;
    private readonly IMongoCollection<AssistantMessage> _messages;
    private readonly ICurrentUser _currentUser;
    private readonly ISender _sender;
    private readonly IAssistantGuardrailsService _guardrails;
    private readonly IGeminiAiService _gemini;
    private readonly IAssistantToolRegistry _toolRegistry;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProcessVoiceTurnHandler(
        IMongoDatabase database,
        ICurrentUser currentUser,
        ISender sender,
        IAssistantGuardrailsService guardrails,
        IGeminiAiService gemini,
        IAssistantToolRegistry toolRegistry)
    {
        _conversations = database.GetCollection<AssistantConversation>("assistant_conversations");
        _messages = database.GetCollection<AssistantMessage>("assistant_messages");
        _currentUser = currentUser;
        _sender = sender;
        _guardrails = guardrails;
        _gemini = gemini;
        _toolRegistry = toolRegistry;
    }

    public async Task<Result<VoiceTurnResult>> Handle(
        ProcessVoiceTurnCommand request, CancellationToken ct)
    {
        // 1. Enforce Rate Limiting
        var rateLimitCheck = _guardrails.CheckRateLimit(_currentUser.UserId);
        if (!rateLimitCheck.IsSuccess)
            return Result<VoiceTurnResult>.Failure(rateLimitCheck.Error ?? "Rate limit exceeded.");

        // 2. Validate and Sanitize User Content
        var contentValidation = _guardrails.SanitizeAndValidateInput(request.Transcript);
        if (!contentValidation.IsSuccess)
            return Result<VoiceTurnResult>.Failure(contentValidation.Error ?? "Invalid input.");

        var sanitizedTranscript = contentValidation.Value!;

        var convFilter = Builders<AssistantConversation>.Filter.Eq(c => c.Id, request.ConversationId) &
                         Builders<AssistantConversation>.Filter.Eq(c => c.UserId, _currentUser.UserId);

        var conv = await _conversations.Find(convFilter).FirstOrDefaultAsync(ct);
        if (conv is null)
        {
            return Result<VoiceTurnResult>.Failure("Conversation not found or access denied.");
        }

        // 3. Find most recent active Proposed Action message for Multi-Turn Conversational Memory
        var recentMessages = await _messages.Find(
            Builders<AssistantMessage>.Filter.Eq(m => m.ConversationId, request.ConversationId) &
            Builders<AssistantMessage>.Filter.Eq(m => m.UserId, _currentUser.UserId)
        ).Sort(Builders<AssistantMessage>.Sort.Descending(m => m.CreatedAt))
         .Limit(6)
         .ToListAsync(ct);

        var lastProposedMsg = recentMessages.FirstOrDefault(m => m.ActionStatus == "Proposed");

        // 4. Record User Voice Message
        var userMsg = new AssistantMessage
        {
            ConversationId = request.ConversationId,
            UserId = _currentUser.UserId,
            Role = "user",
            Content = sanitizedTranscript,
            CreatedAt = DateTime.UtcNow
        };
        await _messages.InsertOneAsync(userMsg, cancellationToken: ct);

        // 5. Classify intent and dispatch turn via Hybrid Architecture (Gemini Function Calling + Fast Path)
        var lower = sanitizedTranscript.Trim().ToLowerInvariant();

        string assistantReply = string.Empty;
        string? actionType = null;
        string? actionStatus = null;
        string? actionSummary = null;
        string? actionPayloadJson = null;
        string? toolName = null;

        // --- Fast Path: Multi-Turn Modification on Existing Proposed Action ---
        if (lastProposedMsg != null && IsMultiTurnFollowUp(lower))
        {
            toolName = lastProposedMsg.ActionType ?? "ProposeCreateTransaction";

            if (Regex.IsMatch(lower, @"^(?:yes|confirm|record|save|do it|go ahead|proceed|sure|ok|okay)(?:\s+(?:please|it|now))?$", RegexOptions.IgnoreCase))
            {
                var updateProposed = Builders<AssistantMessage>.Update.Set(m => m.ActionStatus, "Confirmed");
                await _messages.UpdateOneAsync(Builders<AssistantMessage>.Filter.Eq(m => m.Id, lastProposedMsg.Id), updateProposed, cancellationToken: ct);
                assistantReply = "Action confirmed and saved successfully!";
                actionType = lastProposedMsg.ActionType;
                actionStatus = "Confirmed";
                actionSummary = lastProposedMsg.ActionSummary;
            }
            else if (Regex.IsMatch(lower, @"^(?:no|cancel|discard|nevermind|don't|stop)(?:\s+(?:it|that|please))?$", RegexOptions.IgnoreCase))
            {
                var updateProposed = Builders<AssistantMessage>.Update.Set(m => m.ActionStatus, "Cancelled");
                await _messages.UpdateOneAsync(Builders<AssistantMessage>.Filter.Eq(m => m.Id, lastProposedMsg.Id), updateProposed, cancellationToken: ct);
                assistantReply = "Proposed action was cancelled.";
                actionType = lastProposedMsg.ActionType;
                actionStatus = "Cancelled";
            }
            else if (lastProposedMsg.ActionType == "TransferFunds")
            {
                var amount = HasExplicitNumber(sanitizedTranscript) ? ParseAmount(sanitizedTranscript) : 500m;
                var (fromAcc, toAcc) = ExtractTransferAccounts(sanitizedTranscript);

                var cmd = new ProposeTransferCommand(amount, fromAcc, toAcc, null, sanitizedTranscript);
                var result = await _sender.Send(cmd, ct);
                if (result.IsSuccess)
                {
                    actionType = "TransferFunds";
                    actionStatus = "Proposed";
                    actionSummary = result.Value!.Summary;
                    actionPayloadJson = JsonSerializer.Serialize(result.Value!.Payload);
                    assistantReply = $"I've updated the transfer: {result.Value!.Summary}. Please confirm below to execute it.";
                }
                else
                {
                    assistantReply = "I could not update that transfer.";
                }
            }
            else
            {
                ProposedCreateTransactionPayload? existingPayload = null;
                if (!string.IsNullOrEmpty(lastProposedMsg.ActionPayloadJson))
                {
                    try
                    {
                        existingPayload = JsonSerializer.Deserialize<ProposedCreateTransactionPayload>(lastProposedMsg.ActionPayloadJson, _jsonOptions);
                    }
                    catch { }
                }

                var isIncomeFollowUp = Regex.IsMatch(lower, @"income|deposit|salary|earned|bonus", RegexOptions.IgnoreCase);
                var extractedType = isIncomeFollowUp ? "Income" : (existingPayload?.Type ?? "Expense");
                var extractedAccount = ExtractAccountName(sanitizedTranscript) ?? existingPayload?.AccountName ?? "Default Account";
                var extractedAmount = HasExplicitNumber(sanitizedTranscript) ? ParseAmount(sanitizedTranscript) : (existingPayload?.Amount ?? 100m);
                var extractedCategory = HasCategoryKeyword(sanitizedTranscript) ? ParseCategory(sanitizedTranscript, extractedType == "Income") : (existingPayload?.CategoryName ?? (extractedType == "Income" ? "Salary" : "General Expense"));
                var extractedTitle = existingPayload?.Title ?? extractedCategory;

                var cmd = new ProposeCreateTransactionCommand(
                    Amount: extractedAmount,
                    Category: extractedCategory,
                    Account: extractedAccount,
                    Title: extractedTitle,
                    Note: sanitizedTranscript,
                    Type: extractedType
                );

                var result = await _sender.Send(cmd, ct);
                if (result.IsSuccess)
                {
                    actionType = "AddTransaction";
                    actionStatus = "Proposed";
                    actionSummary = result.Value!.Summary;
                    actionPayloadJson = JsonSerializer.Serialize(result.Value!.Payload);
                    assistantReply = $"I've updated the transaction: {result.Value!.Summary}. Please confirm below to record it.";
                }
                else
                {
                    assistantReply = "I could not update that transaction.";
                }
            }
        }
        else
        {
            // --- Attempt Gemini AI Function Calling Engine (If Configured) ---
            bool geminiHandled = false;
            if (_gemini.IsConfigured)
            {
                var tools = _toolRegistry.GetToolDefinitions();
                var geminiResult = await _gemini.ProcessTurnAsync(sanitizedTranscript, recentMessages, tools, ct);

                if (geminiResult.IsSuccess)
                {
                    if (!string.IsNullOrWhiteSpace(geminiResult.FunctionCallName))
                    {
                        toolName = geminiResult.FunctionCallName;
                        using var argsDoc = JsonDocument.Parse(geminiResult.FunctionCallArgsJson ?? "{}");
                        var toolExecRes = await _toolRegistry.ExecuteToolAsync(toolName, argsDoc.RootElement, ct);

                        if (toolExecRes.IsSuccess)
                        {
                            geminiHandled = true;
                            var toolVal = toolExecRes.Value;

                            if (toolVal is ProposedActionDto<ProposedCreateTransactionPayload> txProp)
                            {
                                actionType = txProp.ActionType;
                                actionStatus = txProp.Status;
                                actionSummary = txProp.Summary;
                                actionPayloadJson = JsonSerializer.Serialize(txProp.Payload);
                                assistantReply = $"I have prepared a transaction: {txProp.Summary}. Please confirm below to record it.";
                            }
                            else if (toolVal is ProposedActionDto<ProposedTransferPayload> trProp)
                            {
                                actionType = trProp.ActionType;
                                actionStatus = trProp.Status;
                                actionSummary = trProp.Summary;
                                actionPayloadJson = JsonSerializer.Serialize(trProp.Payload);
                                assistantReply = $"I have prepared a transfer: {trProp.Summary}. Please confirm below to execute it.";
                            }
                            else if (toolVal is ProposedActionDto<ProposedCreateAccountPayload> accProp)
                            {
                                actionType = accProp.ActionType;
                                actionStatus = accProp.Status;
                                actionSummary = accProp.Summary;
                                actionPayloadJson = JsonSerializer.Serialize(accProp.Payload);
                                assistantReply = $"I have prepared an account creation: {accProp.Summary}. Please confirm below.";
                            }
                            else if (toolVal is PortfolioOrAccountBalanceResult bal)
                            {
                                assistantReply = $"Your total portfolio balance is ৳{bal.TotalBalance:N2} across {bal.AccountCount} active accounts.";
                            }
                            else if (toolVal is TopSpendingExpensesResult top)
                            {
                                assistantReply = $"You have spent ৳{top.TotalSpentInPeriod:N2} this month." +
                                                 (top.Expenses.Count > 0 ? " Top items: " + string.Join(", ", top.Expenses.Select(e => $"{e.Title} (৳{e.Amount:N0})")) : "");
                            }
                            else if (toolVal is SavingsPlansStatusResult sav)
                            {
                                assistantReply = $"You have {sav.PlanCount} active savings goals with ৳{sav.TotalCurrentAmount:N2} saved toward a target of ৳{sav.TotalTargetAmount:N2}.";
                            }
                            else
                            {
                                assistantReply = JsonSerializer.Serialize(toolVal, _jsonOptions);
                            }
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(geminiResult.ReplyText))
                    {
                        geminiHandled = true;
                        assistantReply = geminiResult.ReplyText;
                    }
                }
            }

            // --- Deterministic Fast-Path Local Fallback (If Gemini is unconfigured, errored, or bypassed) ---
            if (!geminiHandled)
            {
                if (lower.Contains("overview") || lower.Contains("summary") || lower.Contains("financial status"))
                {
                    toolName = "GetPortfolioOrAccountBalance";
                    var balanceRes = await _sender.Send(new GetPortfolioOrAccountBalanceQuery(), ct);
                    var expensesRes = await _sender.Send(new GetTopSpendingExpensesQuery("this_month", 3), ct);

                    var totalBal = balanceRes.IsSuccess ? balanceRes.Value!.TotalBalance : 0m;
                    var accCount = balanceRes.IsSuccess ? balanceRes.Value!.AccountCount : 0;
                    var spent = expensesRes.IsSuccess ? expensesRes.Value!.TotalSpentInPeriod : 0m;

                    assistantReply = $"Your total portfolio balance is ৳{totalBal:N2} across {accCount} active accounts, with ৳{spent:N2} spent so far this month.";
                    if (expensesRes.IsSuccess && expensesRes.Value!.Expenses.Count > 0)
                    {
                        assistantReply += " Top expenses include: " + string.Join(", ", expensesRes.Value.Expenses.Select(e => $"{e.Title} (৳{e.Amount:N0})"));
                    }
                }
                else if (lower.Contains("balance") || lower.Contains("how much") || lower.Contains("portfolio"))
                {
                    toolName = "GetPortfolioOrAccountBalance";
                    var result = await _sender.Send(new GetPortfolioOrAccountBalanceQuery(), ct);
                    if (result.IsSuccess)
                    {
                        var val = result.Value!;
                        assistantReply = $"Your current total portfolio balance is ৳{val.TotalBalance:N2} across {val.AccountCount} active accounts.";
                    }
                    else
                    {
                        assistantReply = "I was unable to retrieve your account balance right now.";
                    }
                }
                else if (lower.Contains("account") && (lower.Contains("list") || lower.Contains("show") || lower.Contains("what") || lower.Contains("all")))
                {
                    toolName = "GetActiveAccountsSummary";
                    var result = await _sender.Send(new GetActiveAccountsSummaryQuery(false), ct);
                    if (result.IsSuccess)
                    {
                        var val = result.Value!;
                        assistantReply = $"You have {val.ActiveAccountCount} active accounts with a net balance of ৳{val.TotalPortfolioBalance:N2}.";
                        if (val.Accounts.Count > 0)
                        {
                            assistantReply += " Accounts: " + string.Join(", ", val.Accounts.Select(a => $"{a.Name}: ৳{a.Balance:N0}"));
                        }
                    }
                    else
                    {
                        assistantReply = "I could not retrieve your accounts summary.";
                    }
                }
                else if (
                    Regex.IsMatch(lower, @"(?:transfer|send|move|wire|shift|withdraw)\s+(?:৳|bdt|tk|\$)?\s*([0-9,]+)", RegexOptions.IgnoreCase) ||
                    (lower.Contains("transfer") && (lower.Contains("from") || lower.Contains("to") || lower.Contains("between")))
                )
                {
                    toolName = "ProposeTransfer";
                    var amount = ParseAmount(request.Transcript);
                    var (fromAcc, toAcc) = ExtractTransferAccounts(request.Transcript);

                    var cmd = new ProposeTransferCommand(amount, fromAcc, toAcc, null, request.Transcript.Trim());
                    var result = await _sender.Send(cmd, ct);
                    if (result.IsSuccess)
                    {
                        actionType = "TransferFunds";
                        actionStatus = "Proposed";
                        actionSummary = result.Value!.Summary;
                        actionPayloadJson = JsonSerializer.Serialize(result.Value!.Payload);
                        assistantReply = $"I have prepared a transfer: {result.Value!.Summary}. Please confirm below to execute it.";
                    }
                    else
                    {
                        assistantReply = "I could not prepare that transfer.";
                    }
                }
                else if (lower.Contains("spending") || lower.Contains("expense") || lower.Contains("spent this month"))
                {
                    toolName = "GetTopSpendingExpenses";
                    var result = await _sender.Send(new GetTopSpendingExpensesQuery("this_month", 5), ct);
                    if (result.IsSuccess)
                    {
                        var val = result.Value!;
                        assistantReply = $"You have spent ৳{val.TotalSpentInPeriod:N2} this month.";
                        if (val.Expenses.Count > 0)
                        {
                            assistantReply += " Top items: " + string.Join(", ", val.Expenses.Select(e => $"{e.Title} for ৳{e.Amount:N0}"));
                        }
                    }
                    else
                    {
                        assistantReply = "I could not retrieve your spending breakdown.";
                    }
                }
                else if (lower.Contains("goal") || lower.Contains("savings") || lower.Contains("plan"))
                {
                    toolName = "GetSavingsPlansStatus";
                    var result = await _sender.Send(new GetSavingsPlansStatusQuery(), ct);
                    if (result.IsSuccess)
                    {
                        var val = result.Value!;
                        assistantReply = $"You have {val.PlanCount} active savings goals with ৳{val.TotalCurrentAmount:N2} saved toward a total target of ৳{val.TotalTargetAmount:N2}.";
                    }
                    else
                    {
                        assistantReply = "I could not retrieve your savings plans status.";
                    }
                }
                else if (
                    Regex.IsMatch(lower, @"(?:add|at|ad|record|log|spent|spend|buy|bought|pay|paid|cost|create|make|insert|new|got|received|deposit|earn|earned|bonus|salary|consulting|dividend|interest|grant)\s*(?:a|an)?\s*(?:transaction|expense|income|entry|salary|deposit|payment)?", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(lower, @"(?:bdt|৳|tk|dollar|\$|\bamount\b)\s*([0-9,]+)", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(lower, @"([0-9,]+)\s*(?:bdt|৳|tk|taka|bucks|dollars|\$)", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(lower, @"(?:transaction|expense|income|salary|deposit)\s*(?:of|for)?\s*([0-9]+)", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(lower, @"[0-9]+\s*(?:for|on|in|to)\s*[a-zA-Z]+", RegexOptions.IgnoreCase)
                )
                {
                    toolName = "ProposeCreateTransaction";
                    var isIncome = Regex.IsMatch(lower, @"salary|income|earned|earn|deposit|bonus|freelance|wage|received|got|reimburse|cashback|dividend|interest|consulting|gift", RegexOptions.IgnoreCase) ||
                                   Regex.IsMatch(lower, @"(?:add|log|record)\s+(?:an?\s+)?income", RegexOptions.IgnoreCase);

                    var txType = isIncome ? "Income" : "Expense";
                    var amount = ParseAmount(request.Transcript);
                    var category = ParseCategory(request.Transcript, isIncome);
                    var title = ParseTitle(request.Transcript, category);
                    var account = ExtractAccountName(request.Transcript) ?? "Default Account";

                    var cmd = new ProposeCreateTransactionCommand(
                        Amount: amount,
                        Category: category,
                        Account: account,
                        Title: title,
                        Note: request.Transcript.Trim(),
                        Type: txType
                    );

                    var result = await _sender.Send(cmd, ct);
                    if (result.IsSuccess)
                    {
                        actionType = "AddTransaction";
                        actionStatus = "Proposed";
                        actionSummary = result.Value!.Summary;
                        actionPayloadJson = JsonSerializer.Serialize(result.Value!.Payload);
                        assistantReply = $"I have prepared a transaction: {result.Value!.Summary}. Please confirm below to record it.";
                    }
                    else
                    {
                        assistantReply = "I could not prepare that transaction.";
                    }
                }
                else
                {
                    assistantReply = "I can help you check your balances, review monthly spending, inspect savings plans, log income & expenses, or transfer funds between accounts. What would you like to do?";
                }
            }
        }

        // 6. Save Assistant Message
        var assistantMsg = new AssistantMessage
        {
            ConversationId = request.ConversationId,
            UserId = _currentUser.UserId,
            Role = "assistant",
            Content = assistantReply,
            ActionType = actionType,
            ActionStatus = actionStatus,
            ActionSummary = actionSummary,
            ActionPayloadJson = actionPayloadJson,
            CreatedAt = DateTime.UtcNow
        };
        await _messages.InsertOneAsync(assistantMsg, cancellationToken: ct);

        // 7. Update Conversation timestamp
        var update = Builders<AssistantConversation>.Update
            .Set(c => c.LastMessageAt, DateTime.UtcNow)
            .Set(c => c.ModifiedAt, DateTime.UtcNow);
        await _conversations.UpdateOneAsync(convFilter, update, cancellationToken: ct);

        var voiceResult = new VoiceTurnResult(
            MessageId: assistantMsg.Id,
            ConversationId: request.ConversationId,
            UserTranscript: request.Transcript,
            AssistantReply: assistantReply,
            ActionType: actionType,
            ActionStatus: actionStatus,
            ActionSummary: actionSummary,
            ActionPayloadJson: actionPayloadJson,
            ToolName: toolName
        );

        return Result<VoiceTurnResult>.Success(voiceResult);
    }

    private static bool IsMultiTurnFollowUp(string lower)
    {
        return Regex.IsMatch(lower, @"^(?:yes|confirm|record|save|do it|go ahead|proceed|sure|ok|okay)(?:\s+(?:please|it|now))?$", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(lower, @"^(?:no|cancel|discard|nevermind|don't|stop)(?:\s+(?:it|that|please))?$", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(lower, @"(?:from|in|using|via|with|paid\s+(?:from|with|in)|account\s+(?:is|to)|change\s+account\s+to|it\s+would\s+be\s+from|from\s+account)\s+([a-zA-Z0-9\s]+)", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(lower, @"\b(?:cash|bank|bkash|nagad|rocket|wallet|card)\b", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(lower, @"(?:actually|change\s+amount|make\s+it|amount\s+is)\s+[0-9]+", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(lower, @"(?:change\s+category|category\s+is)\s+[a-zA-Z]+", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(lower, @"(?:income|expense|salary|transfer)", RegexOptions.IgnoreCase);
    }

    private static (string fromAccount, string toAccount) ExtractTransferAccounts(string text)
    {
        var match = Regex.Match(text, @"(?:from\s+([a-zA-Z0-9\s]+?)\s+to\s+([a-zA-Z0-9\s]+))|(?:to\s+([a-zA-Z0-9\s]+?)\s+from\s+([a-zA-Z0-9\s]+))", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            if (!string.IsNullOrWhiteSpace(match.Groups[1].Value) && !string.IsNullOrWhiteSpace(match.Groups[2].Value))
            {
                return (CleanAccountName(match.Groups[1].Value), CleanAccountName(match.Groups[2].Value));
            }
            if (!string.IsNullOrWhiteSpace(match.Groups[3].Value) && !string.IsNullOrWhiteSpace(match.Groups[4].Value))
            {
                return (CleanAccountName(match.Groups[4].Value), CleanAccountName(match.Groups[3].Value));
            }
        }

        var fromMatch = Regex.Match(text, @"(?:from)\s+([a-zA-Z0-9\s]+?)(?:\s*(?:to|into|please|now|$)|$)", RegexOptions.IgnoreCase);
        var toMatch = Regex.Match(text, @"(?:to|into)\s+([a-zA-Z0-9\s]+?)(?:\s*(?:from|please|now|$)|$)", RegexOptions.IgnoreCase);

        var from = fromMatch.Success ? CleanAccountName(fromMatch.Groups[1].Value) : "Bank Account";
        var to = toMatch.Success ? CleanAccountName(toMatch.Groups[1].Value) : "Cash";

        return (from, to);
    }

    private static string CleanAccountName(string raw)
    {
        var val = raw.Trim();
        if (val.Equals("cash", StringComparison.OrdinalIgnoreCase)) return "Cash";
        if (val.Equals("bkash", StringComparison.OrdinalIgnoreCase)) return "bKash";
        if (val.Equals("nagad", StringComparison.OrdinalIgnoreCase)) return "Nagad";
        if (val.Equals("rocket", StringComparison.OrdinalIgnoreCase)) return "Rocket";
        if (val.Equals("bank", StringComparison.OrdinalIgnoreCase) || val.Equals("bank account", StringComparison.OrdinalIgnoreCase)) return "Bank Account";
        return char.ToUpper(val[0]) + val[1..].ToLower();
    }

    private static string? ExtractAccountName(string text)
    {
        var match = Regex.Match(text, @"(?:from|in|using|via|with|paid\s+(?:from|with|in)|account\s+(?:is|to)|change\s+account\s+to|it\s+would\s+be\s+from|from\s+account|to\s+account|into\s+account)\s+([a-zA-Z0-9\s]+?)(?:\s*(?:please|instead|now|$)|$)", RegexOptions.IgnoreCase);
        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
        {
            return CleanAccountName(match.Groups[1].Value);
        }

        if (Regex.IsMatch(text, @"\bcash\b", RegexOptions.IgnoreCase)) return "Cash";
        if (Regex.IsMatch(text, @"\bbkash\b", RegexOptions.IgnoreCase)) return "bKash";
        if (Regex.IsMatch(text, @"\bnagad\b", RegexOptions.IgnoreCase)) return "Nagad";
        if (Regex.IsMatch(text, @"\brocket\b", RegexOptions.IgnoreCase)) return "Rocket";
        if (Regex.IsMatch(text, @"\bbank\b", RegexOptions.IgnoreCase)) return "Bank Account";
        return null;
    }

    private static bool HasExplicitNumber(string text) =>
        Regex.IsMatch(text, @"[0-9]+");

    private static bool HasCategoryKeyword(string text) =>
        Regex.IsMatch(text, @"food|foot|feed|dining|lunch|dinner|breakfast|snack|transport|bill|utilities|entertainment|health|shopping|housing|education|salary|invest|freelance|bonus|gift|dividend", RegexOptions.IgnoreCase);

    private static decimal ParseAmount(string text)
    {
        var match = Regex.Match(text, @"(?:bdt|৳|tk|taka|dollar|\$|\bamount\b|\bof\b|\bfor\b)?\s*([0-9]+(?:\.[0-9]{1,2})?)\s*(?:bdt|৳|tk|taka|dollar|\$)?", RegexOptions.IgnoreCase);
        if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var num) && num > 0)
        {
            return num;
        }

        var genericMatch = Regex.Match(text, @"\b([0-9]+(?:\.[0-9]{1,2})?)\b");
        if (genericMatch.Success && decimal.TryParse(genericMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var gNum) && gNum > 0)
        {
            return gNum;
        }

        return 100m;
    }

    private static string ParseCategory(string text, bool isIncome)
    {
        if (isIncome)
        {
            if (Regex.IsMatch(text, @"freelance|upwork|fiverr|client|contract|consulting", RegexOptions.IgnoreCase)) return "Freelance";
            if (Regex.IsMatch(text, @"bonus|reward|gift|presents", RegexOptions.IgnoreCase)) return "Bonus";
            if (Regex.IsMatch(text, @"invest|dividend|stock|interest|profit|crypto", RegexOptions.IgnoreCase)) return "Investments";
            if (Regex.IsMatch(text, @"refund|reimburse|cashback", RegexOptions.IgnoreCase)) return "Reimbursement";
            return "Salary";
        }

        if (Regex.IsMatch(text, @"food|foot|feed|dining|lunch|dinner|breakfast|snack|snacks|coffee|tea|cafe|restaurant|burger|pizza|grocery|groceries|market|fruit|vegetables|sweet|sweets", RegexOptions.IgnoreCase)) return "Food & Dining";
        if (Regex.IsMatch(text, @"transport|transportation|bus|train|uber|pathao|taxi|auto|rickshaw|metro|ticket|fuel|gas|petrol|octane|diesel|cng|fare|ride", RegexOptions.IgnoreCase)) return "Transportation";
        if (Regex.IsMatch(text, @"utility|utilities|bill|bills|electricity|current|gas|water|wasa|desco|internet|wifi|broadband|recharge|mobile|phone", RegexOptions.IgnoreCase)) return "Utilities";
        if (Regex.IsMatch(text, @"entertainment|movie|cinema|film|netflix|spotify|game|gaming|fun|concert|theatre|outing", RegexOptions.IgnoreCase)) return "Entertainment";
        if (Regex.IsMatch(text, @"health|healthcare|medical|medicine|medicines|doctor|hospital|clinic|pharmacy|drug|drugs|checkup|dentist", RegexOptions.IgnoreCase)) return "Healthcare";
        if (Regex.IsMatch(text, @"shopping|dress|cloth|clothes|clothing|shirt|pants|shoe|shoes|bag|mall|daraz|amazon|store|gadget|electronics", RegexOptions.IgnoreCase)) return "Shopping";
        if (Regex.IsMatch(text, @"housing|house|rent|apartment|flat|maintenance", RegexOptions.IgnoreCase)) return "Housing";
        if (Regex.IsMatch(text, @"education|tuition|course|class|training|book|books|stationery|school|college|university|exam", RegexOptions.IgnoreCase)) return "Education";
        if (Regex.IsMatch(text, @"salary|income|wage|paycheck|bonus|freelance|earnings", RegexOptions.IgnoreCase)) return "Salary";
        if (Regex.IsMatch(text, @"invest|investment|stock|stocks|crypto|savings|deposit", RegexOptions.IgnoreCase)) return "Investments";
        return "General Expense";
    }

    private static string ParseTitle(string text, string category)
    {
        var forMatch = Regex.Match(text, @"(?:for|on|in|from|as)\s+([a-zA-Z\s]+?)(?:\s*(?:of|at|with|to|bdt|৳|tk|\$|[0-9]|$)|$)", RegexOptions.IgnoreCase);
        if (forMatch.Success && !string.IsNullOrWhiteSpace(forMatch.Groups[1].Value))
        {
            var raw = forMatch.Groups[1].Value.Trim();
            if (raw.Equals("foot", StringComparison.OrdinalIgnoreCase) || raw.Equals("food", StringComparison.OrdinalIgnoreCase))
                return "Food & Dining";
            return char.ToUpper(raw[0]) + raw[1..].ToLower();
        }
        return category;
    }
}
