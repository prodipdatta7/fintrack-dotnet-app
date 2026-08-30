using System.Text.Json;
using FinTrack.BuildingBlocks;
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
using MediatR;

namespace FinTrack.Modules.Assistant.Services;

public sealed class AssistantToolRegistry : IAssistantToolRegistry
{
    private readonly ISender _sender;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AssistantToolRegistry(ISender sender)
    {
        _sender = sender;
    }

    public IReadOnlyList<ToolDefinitionDto> GetToolDefinitions() =>
    [
        new ToolDefinitionDto(
            Name: "GetPortfolioOrAccountBalance",
            Description: "Get the total net balance of all active accounts or check the balance of a specific account by name or id.",
            Category: "Read",
            Parameters: new Dictionary<string, ToolParameterPropertyDto>
            {
                ["accountId"] = new("string", "Optional ID of the account to check"),
                ["accountName"] = new("string", "Optional name or partial name of the account to check (e.g. 'Cash', 'City Bank')")
            },
            RequiredParameters: []),

        new ToolDefinitionDto(
            Name: "GetActiveAccountsSummary",
            Description: "Get a list of all active user accounts with types, balances, currencies, and total count.",
            Category: "Read",
            Parameters: new Dictionary<string, ToolParameterPropertyDto>
            {
                ["includeClosed"] = new("boolean", "Whether to include closed/archived accounts")
            },
            RequiredParameters: []),

        new ToolDefinitionDto(
            Name: "GetTopSpendingExpenses",
            Description: "Get the top expense transactions for a given period sorted by amount descending.",
            Category: "Read",
            Parameters: new Dictionary<string, ToolParameterPropertyDto>
            {
                ["period"] = new("string", "Timeframe period (e.g. 'this_month', 'last_month', 'this_week', 'this_year', 'all')", EnumValues: ["this_month", "last_month", "this_week", "this_year", "all"]),
                ["limit"] = new("integer", "Number of top expenses to return (default 5)")
            },
            RequiredParameters: []),

        new ToolDefinitionDto(
            Name: "GetCategorySpendingVsBudget",
            Description: "Get total expenses spent in a specific category for a period compared to its monthly budget limit.",
            Category: "Read",
            Parameters: new Dictionary<string, ToolParameterPropertyDto>
            {
                ["category"] = new("string", "Category name or ID (e.g. 'Food & Dining', 'Utilities')", IsRequired: true),
                ["period"] = new("string", "Timeframe period (e.g. 'this_month', 'last_month', 'this_year')", EnumValues: ["this_month", "last_month", "this_year", "all"])
            },
            RequiredParameters: ["category"]),

        new ToolDefinitionDto(
            Name: "GetSavingsPlansStatus",
            Description: "Get progress, current amounts, remaining targets, and deadlines for all savings plans or a specific plan.",
            Category: "Read",
            Parameters: new Dictionary<string, ToolParameterPropertyDto>
            {
                ["planId"] = new("string", "Optional savings plan ID"),
                ["planName"] = new("string", "Optional savings plan name to filter by")
            },
            RequiredParameters: []),

        new ToolDefinitionDto(
            Name: "ProposeCreateTransaction",
            Description: "Propose creating a new transaction. Returns a confirmation card for the user to confirm before saving.",
            Category: "ProposedWrite",
            Parameters: new Dictionary<string, ToolParameterPropertyDto>
            {
                ["amount"] = new("number", "Amount of the transaction in BDT", IsRequired: true),
                ["category"] = new("string", "Category name or ID (e.g. 'Food & Dining', 'Groceries')", IsRequired: true),
                ["account"] = new("string", "Account name or ID (e.g. 'Cash', 'bKash', 'BRAC Bank')", IsRequired: true),
                ["title"] = new("string", "Optional title or description for the transaction"),
                ["type"] = new("string", "Transaction type: 'Expense' or 'Income' (default: Expense)", EnumValues: ["Expense", "Income"]),
                ["date"] = new("string", "ISO date string if other than current time"),
                ["note"] = new("string", "Optional extra note or details")
            },
            RequiredParameters: ["amount", "category", "account"]),

        new ToolDefinitionDto(
            Name: "ProposeCreateAccount",
            Description: "Propose creating a new financial account. Returns a confirmation card for the user to confirm.",
            Category: "ProposedWrite",
            Parameters: new Dictionary<string, ToolParameterPropertyDto>
            {
                ["name"] = new("string", "Name of the account (e.g. 'Standard Chartered Bank', 'Nagad')", IsRequired: true),
                ["type"] = new("string", "Account type ('Bank', 'MFS', 'Cash', 'Credit')", IsRequired: true, EnumValues: ["Bank", "MFS", "Cash", "Credit"]),
                ["initialBalance"] = new("number", "Starting balance amount"),
                ["currency"] = new("string", "Currency code (default 'BDT')"),
                ["color"] = new("string", "Hex color string (e.g. '#6366f1')")
            },
            RequiredParameters: ["name", "type"]),

        new ToolDefinitionDto(
            Name: "ProposeCreateCategory",
            Description: "Propose creating a new expense or income category. Returns a confirmation card for the user to confirm.",
            Category: "ProposedWrite",
            Parameters: new Dictionary<string, ToolParameterPropertyDto>
            {
                ["name"] = new("string", "Category name (e.g. 'Gym & Fitness')", IsRequired: true),
                ["type"] = new("string", "Category type ('Expense' or 'Income')", EnumValues: ["Expense", "Income"]),
                ["budgetLimit"] = new("number", "Optional monthly spending limit (0 = no limit)"),
                ["icon"] = new("string", "Icon name"),
                ["color"] = new("string", "Hex color string")
            },
            RequiredParameters: ["name"]),

        new ToolDefinitionDto(
            Name: "ProposeCreateTag",
            Description: "Propose creating a new tag label for tagging transactions. Returns a confirmation card.",
            Category: "ProposedWrite",
            Parameters: new Dictionary<string, ToolParameterPropertyDto>
            {
                ["name"] = new("string", "Tag name (e.g. 'Tax-Deductible', 'Vacation')", IsRequired: true)
            },
            RequiredParameters: ["name"]),

        new ToolDefinitionDto(
            Name: "ProposeCreateSavingsPlan",
            Description: "Propose creating a new savings goal/plan. Returns a confirmation card for user confirmation.",
            Category: "ProposedWrite",
            Parameters: new Dictionary<string, ToolParameterPropertyDto>
            {
                ["name"] = new("string", "Goal or plan title (e.g. 'Emergency Fund', 'New Laptop')", IsRequired: true),
                ["targetAmount"] = new("number", "Target goal amount in BDT", IsRequired: true),
                ["targetDate"] = new("string", "Target completion deadline date (ISO string)"),
                ["initialAmount"] = new("number", "Initial deposited amount"),
                ["color"] = new("string", "Hex color code")
            },
            RequiredParameters: ["name", "targetAmount"]),

        new ToolDefinitionDto(
            Name: "ProposeTransfer",
            Description: "Propose transferring funds between two user accounts (e.g. from Bank Account to bKash, Cash to Bank). Returns a confirmation card.",
            Category: "ProposedWrite",
            Parameters: new Dictionary<string, ToolParameterPropertyDto>
            {
                ["amount"] = new("number", "Amount of funds to transfer in BDT", IsRequired: true),
                ["fromAccount"] = new("string", "Source account name or ID to transfer from", IsRequired: true),
                ["toAccount"] = new("string", "Destination account name or ID to transfer to", IsRequired: true),
                ["date"] = new("string", "Optional ISO date string"),
                ["note"] = new("string", "Optional transfer note")
            },
            RequiredParameters: ["amount", "fromAccount", "toAccount"])
    ];

    public async Task<Result<object>> ExecuteToolAsync(string toolName, JsonElement arguments, CancellationToken ct)
    {
        var normalizedName = toolName.Trim();

        switch (normalizedName.ToLowerInvariant())
        {
            case "getportfoliooraccountbalance":
            case "get_portfolio_or_account_balance":
            case "getbalance":
            case "get_balance":
            {
                var accountId = arguments.TryGetProperty("accountId", out var pId) ? pId.GetString() : null;
                var accountName = arguments.TryGetProperty("accountName", out var pName) ? pName.GetString() : null;
                var query = new GetPortfolioOrAccountBalanceQuery(accountId, accountName);
                var result = await _sender.Send(query, ct);
                return result.IsSuccess ? Result<object>.Success(result.Value!) : Result<object>.Failure(result.Error!);
            }

            case "getactiveaccountssummary":
            case "get_active_accounts_summary":
            case "getaccounts":
            case "get_accounts":
            {
                var includeClosed = arguments.TryGetProperty("includeClosed", out var pInc) && pInc.GetBoolean();
                var query = new GetActiveAccountsSummaryQuery(includeClosed);
                var result = await _sender.Send(query, ct);
                return result.IsSuccess ? Result<object>.Success(result.Value!) : Result<object>.Failure(result.Error!);
            }

            case "gettopspendingexpenses":
            case "get_top_spending_expenses":
            case "gettopexpenses":
            case "get_top_expenses":
            {
                var period = arguments.TryGetProperty("period", out var pPer) ? pPer.GetString() : "this_month";
                var limit = arguments.TryGetProperty("limit", out var pLim) && pLim.TryGetInt32(out var lVal) ? lVal : 5;
                var query = new GetTopSpendingExpensesQuery(period, limit);
                var result = await _sender.Send(query, ct);
                return result.IsSuccess ? Result<object>.Success(result.Value!) : Result<object>.Failure(result.Error!);
            }

            case "getcategoryspendingvsbudget":
            case "get_category_spending_vs_budget":
            case "getcategoryspend":
            case "get_category_spend":
            {
                var category = arguments.TryGetProperty("category", out var pCat) ? pCat.GetString() ?? string.Empty : string.Empty;
                var period = arguments.TryGetProperty("period", out var pPer) ? pPer.GetString() : "this_month";
                var query = new GetCategorySpendingVsBudgetQuery(category, period);
                var result = await _sender.Send(query, ct);
                return result.IsSuccess ? Result<object>.Success(result.Value!) : Result<object>.Failure(result.Error!);
            }

            case "getsavingsplansstatus":
            case "get_savings_plans_status":
            case "getsavingsplanstatus":
            case "get_savings_plan_status":
            {
                var planId = arguments.TryGetProperty("planId", out var pId) ? pId.GetString() : null;
                var planName = arguments.TryGetProperty("planName", out var pName) ? pName.GetString() : null;
                var query = new GetSavingsPlansStatusQuery(planId, planName);
                var result = await _sender.Send(query, ct);
                return result.IsSuccess ? Result<object>.Success(result.Value!) : Result<object>.Failure(result.Error!);
            }

            case "proposecreatetransaction":
            case "propose_create_transaction":
            case "addtransaction":
            case "add_transaction":
            {
                var amount = arguments.TryGetProperty("amount", out var pAmt) && pAmt.TryGetDecimal(out var aVal) ? aVal : 0m;
                var category = arguments.TryGetProperty("category", out var pCat) ? pCat.GetString() ?? string.Empty : string.Empty;
                var account = arguments.TryGetProperty("account", out var pAcc) ? pAcc.GetString() ?? string.Empty : string.Empty;
                var title = arguments.TryGetProperty("title", out var pTitle) ? pTitle.GetString() : null;
                var note = arguments.TryGetProperty("note", out var pNote) ? pNote.GetString() : null;
                var type = arguments.TryGetProperty("type", out var pType) ? pType.GetString() : "Expense";
                DateTime? date = arguments.TryGetProperty("date", out var pDate) && pDate.TryGetDateTime(out var dt) ? dt : null;

                var command = new ProposeCreateTransactionCommand(amount, category, account, note, date, type, title);
                var result = await _sender.Send(command, ct);
                return result.IsSuccess ? Result<object>.Success(result.Value!) : Result<object>.Failure(result.Error!);
            }

            case "proposecreateaccount":
            case "propose_create_account":
            case "addaccount":
            case "add_account":
            {
                var name = arguments.TryGetProperty("name", out var pName) ? pName.GetString() ?? string.Empty : string.Empty;
                var type = arguments.TryGetProperty("type", out var pType) ? pType.GetString() ?? "Bank" : "Bank";
                var initialBalance = arguments.TryGetProperty("initialBalance", out var pBal) && pBal.TryGetDecimal(out var bVal) ? bVal : 0m;
                var currency = arguments.TryGetProperty("currency", out var pCur) ? pCur.GetString() : "BDT";
                var color = arguments.TryGetProperty("color", out var pCol) ? pCol.GetString() : "#6366f1";

                var command = new ProposeCreateAccountCommand(name, type, initialBalance, currency, color);
                var result = await _sender.Send(command, ct);
                return result.IsSuccess ? Result<object>.Success(result.Value!) : Result<object>.Failure(result.Error!);
            }

            case "proposecreatecategory":
            case "propose_create_category":
            case "addcategory":
            case "add_category":
            {
                var name = arguments.TryGetProperty("name", out var pName) ? pName.GetString() ?? string.Empty : string.Empty;
                var type = arguments.TryGetProperty("type", out var pType) ? pType.GetString() : "Expense";
                var icon = arguments.TryGetProperty("icon", out var pIcon) ? pIcon.GetString() : "tag";
                var color = arguments.TryGetProperty("color", out var pCol) ? pCol.GetString() : "#6366f1";
                var budgetLimit = arguments.TryGetProperty("budgetLimit", out var pLim) && pLim.TryGetDecimal(out var lVal) ? lVal : 0m;

                var command = new ProposeCreateCategoryCommand(name, type, icon, color, budgetLimit);
                var result = await _sender.Send(command, ct);
                return result.IsSuccess ? Result<object>.Success(result.Value!) : Result<object>.Failure(result.Error!);
            }

            case "proposecreatetag":
            case "propose_create_tag":
            case "addtag":
            case "add_tag":
            {
                var name = arguments.TryGetProperty("name", out var pName) ? pName.GetString() ?? string.Empty : string.Empty;
                var command = new ProposeCreateTagCommand(name);
                var result = await _sender.Send(command, ct);
                return result.IsSuccess ? Result<object>.Success(result.Value!) : Result<object>.Failure(result.Error!);
            }

            case "proposecreatesavingsplan":
            case "propose_create_savings_plan":
            case "createsavingsplan":
            case "create_savings_plan":
            {
                var name = arguments.TryGetProperty("name", out var pName) ? pName.GetString() ?? string.Empty : string.Empty;
                var targetAmount = arguments.TryGetProperty("targetAmount", out var pTgt) && pTgt.TryGetDecimal(out var tVal) ? tVal : 0m;
                var initialAmount = arguments.TryGetProperty("initialAmount", out var pIni) && pIni.TryGetDecimal(out var iVal) ? iVal : 0m;
                var color = arguments.TryGetProperty("color", out var pCol) ? pCol.GetString() : "#6366f1";
                DateTime? targetDate = arguments.TryGetProperty("targetDate", out var pDate) && pDate.TryGetDateTime(out var dt) ? dt : null;

                var command = new ProposeCreateSavingsPlanCommand(name, targetAmount, targetDate, initialAmount, color);
                var result = await _sender.Send(command, ct);
                return result.IsSuccess ? Result<object>.Success(result.Value!) : Result<object>.Failure(result.Error!);
            }

            case "proposetransfer":
            case "propose_transfer":
            case "transferfunds":
            case "transfer_funds":
            case "transfer":
            {
                var amount = arguments.TryGetProperty("amount", out var pAmt) && pAmt.TryGetDecimal(out var aVal) ? aVal : 0m;
                var fromAccount = arguments.TryGetProperty("fromAccount", out var pFrom) ? pFrom.GetString() ?? string.Empty : string.Empty;
                var toAccount = arguments.TryGetProperty("toAccount", out var pTo) ? pTo.GetString() ?? string.Empty : string.Empty;
                var note = arguments.TryGetProperty("note", out var pNote) ? pNote.GetString() : null;
                DateTime? date = arguments.TryGetProperty("date", out var pDate) && pDate.TryGetDateTime(out var dt) ? dt : null;

                var command = new ProposeTransferCommand(amount, fromAccount, toAccount, date, note);
                var result = await _sender.Send(command, ct);
                return result.IsSuccess ? Result<object>.Success(result.Value!) : Result<object>.Failure(result.Error!);
            }

            default:
                return Result<object>.Failure($"Tool '{toolName}' is not recognized in the Assistant tool registry.");
        }
    }
}
