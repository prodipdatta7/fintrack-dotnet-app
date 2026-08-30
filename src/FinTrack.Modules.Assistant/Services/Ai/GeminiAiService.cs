using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FinTrack.Modules.Assistant.Domain;
using FinTrack.Modules.Assistant.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinTrack.Modules.Assistant.Services.Ai;

public sealed class GeminiAiService : IGeminiAiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiAiService> _logger;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly bool _enabled;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GeminiAiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiAiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey = configuration["Ai:GeminiApiKey"] 
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? configuration["GEMINI_API_KEY"];

        _model = configuration["Ai:Model"] ?? "gemini-2.0-flash";
        _enabled = configuration.GetValue<bool?>("Ai:Enabled") ?? true;

        var timeoutSec = configuration.GetValue<int?>("Ai:TimeoutSeconds") ?? 10;
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSec);
    }

    public bool IsConfigured => _enabled && !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<GeminiTurnResult> ProcessTurnAsync(
        string userPrompt,
        IReadOnlyList<AssistantMessage> conversationHistory,
        IReadOnlyList<ToolDefinitionDto> tools,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return new GeminiTurnResult(
                IsSuccess: false,
                Error: "Gemini AI is not configured. Falling back to local fast-path engine.");
        }

        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            // 1. Build System Instruction
            var systemInstruction = new JsonObject
            {
                ["parts"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["text"] = "You are FinTrack AI, an intelligent personal financial assistant for FinTrack app.\n" +
                                   "Currency is BDT (৳) by default.\n" +
                                   "Guidelines:\n" +
                                   "1. When user wants to log, add, or record any transaction, expense, income, account, category, or goal, call the corresponding proposed write tool (e.g. ProposeCreateTransaction).\n" +
                                   "2. When user asks about account balance, portfolio total, spending breakdown, budget vs actual, or savings goals, call the corresponding read tool.\n" +
                                   "3. Be concise, friendly, and helpful. For follow-up questions or conversational clarifications, explain clearly."
                    }
                }
            };

            // 2. Build Tools / Function Declarations
            var functionDeclarations = new JsonArray();
            foreach (var tool in tools)
            {
                var propertiesObj = new JsonObject();
                var requiredList = new JsonArray();

                foreach (var (paramName, prop) in tool.Parameters)
                {
                    var paramType = prop.Type.ToLowerInvariant() switch
                    {
                        "number" => "NUMBER",
                        "integer" => "INTEGER",
                        "boolean" => "BOOLEAN",
                        _ => "STRING"
                    };

                    var propNode = new JsonObject
                    {
                        ["type"] = paramType,
                        ["description"] = prop.Description
                    };

                    if (prop.EnumValues != null && prop.EnumValues.Count > 0)
                    {
                        var enumArray = new JsonArray();
                        foreach (var ev in prop.EnumValues) enumArray.Add(ev);
                        propNode["enum"] = enumArray;
                    }

                    propertiesObj[paramName] = propNode;
                }

                foreach (var req in tool.RequiredParameters)
                {
                    requiredList.Add(req);
                }

                var funcDecl = new JsonObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "OBJECT",
                        ["properties"] = propertiesObj,
                        ["required"] = requiredList
                    }
                };

                functionDeclarations.Add(funcDecl);
            }

            var toolsArray = new JsonArray
            {
                new JsonObject
                {
                    ["function_declarations"] = functionDeclarations
                }
            };

            // 3. Build Conversation History Contents
            var contentsArray = new JsonArray();

            // Include last 6 messages for context
            var contextMsgs = conversationHistory.TakeLast(6).ToList();
            foreach (var msg in contextMsgs)
            {
                var role = msg.Role == "assistant" ? "model" : "user";
                if (!string.IsNullOrWhiteSpace(msg.Content))
                {
                    contentsArray.Add(new JsonObject
                    {
                        ["role"] = role,
                        ["parts"] = new JsonArray
                        {
                            new JsonObject { ["text"] = msg.Content }
                        }
                    });
                }
            }

            // Append current prompt
            contentsArray.Add(new JsonObject
            {
                ["role"] = "user",
                ["parts"] = new JsonArray
                {
                    new JsonObject { ["text"] = userPrompt }
                }
            });

            var requestBody = new JsonObject
            {
                ["system_instruction"] = systemInstruction,
                ["tools"] = toolsArray,
                ["contents"] = contentsArray
            };

            var jsonContent = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Gemini API call failed with status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                return new GeminiTurnResult(IsSuccess: false, Error: $"Gemini API error ({response.StatusCode})");
            }

            var responseJsonStr = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJsonStr);
            var root = doc.RootElement;

            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var contentElem) &&
                    contentElem.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    foreach (var part in parts.EnumerateArray())
                    {
                        if (part.TryGetProperty("functionCall", out var functionCall))
                        {
                            var funcName = functionCall.GetProperty("name").GetString();
                            var argsJson = functionCall.TryGetProperty("args", out var args) ? args.GetRawText() : "{}";
                            return new GeminiTurnResult(
                                IsSuccess: true,
                                FunctionCallName: funcName,
                                FunctionCallArgsJson: argsJson);
                        }

                        if (part.TryGetProperty("text", out var textElem))
                        {
                            var text = textElem.GetString();
                            return new GeminiTurnResult(
                                IsSuccess: true,
                                ReplyText: text);
                        }
                    }
                }
            }

            return new GeminiTurnResult(IsSuccess: false, Error: "No usable content generated by Gemini.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception communicating with Gemini AI API");
            return new GeminiTurnResult(IsSuccess: false, Error: ex.Message);
        }
    }
}
