using System.Net;
using FinTrack.Modules.Assistant.Domain;
using FinTrack.Modules.Assistant.Dtos;
using FinTrack.Modules.Assistant.Services.Ai;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Services;

public class GeminiAiServiceTests
{
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public MockHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }

    [Fact]
    public void IsConfigured_ReturnsFalse_WhenApiKeyIsEmpty()
    {
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Ai:GeminiApiKey"] = "",
            ["Ai:Enabled"] = "true"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        var client = new HttpClient(new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));

        var service = new GeminiAiService(client, config, NullLogger<GeminiAiService>.Instance);
        service.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_ReturnsTrue_WhenApiKeyIsProvided()
    {
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Ai:GeminiApiKey"] = "fake-api-key-12345",
            ["Ai:Enabled"] = "true"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        var client = new HttpClient(new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));

        var service = new GeminiAiService(client, config, NullLogger<GeminiAiService>.Instance);
        service.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessTurnAsync_ParsesGeminiFunctionCall_Correctly()
    {
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Ai:GeminiApiKey"] = "fake-api-key-12345",
            ["Ai:Enabled"] = "true"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

        var geminiResponseJson = """
        {
            "candidates": [
                {
                    "content": {
                        "parts": [
                            {
                                "functionCall": {
                                    "name": "ProposeCreateTransaction",
                                    "args": {
                                        "amount": 550,
                                        "category": "Food & Dining",
                                        "account": "Cash",
                                        "title": "Dinner with friends"
                                    }
                                }
                            }
                        ]
                    }
                }
            ]
        }
        """;

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(geminiResponseJson, System.Text.Encoding.UTF8, "application/json")
        };

        var client = new HttpClient(new MockHttpMessageHandler(response));
        var service = new GeminiAiService(client, config, NullLogger<GeminiAiService>.Instance);

        var tools = new List<ToolDefinitionDto>
        {
            new("ProposeCreateTransaction", "Propose creating a transaction", "ProposedWrite",
                new Dictionary<string, ToolParameterPropertyDto>
                {
                    ["amount"] = new("number", "Amount"),
                    ["category"] = new("string", "Category")
                },
                ["amount", "category"])
        };

        var history = new List<AssistantMessage>();

        var result = await service.ProcessTurnAsync("Log 550 for dinner with friends from cash", history, tools);

        result.IsSuccess.Should().BeTrue();
        result.FunctionCallName.Should().Be("ProposeCreateTransaction");
        result.FunctionCallArgsJson.Should().Contain("550");
        result.FunctionCallArgsJson.Should().Contain("Cash");
    }

    [Fact]
    public async Task ProcessTurnAsync_ParsesDirectTextReply_Correctly()
    {
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Ai:GeminiApiKey"] = "fake-api-key-12345",
            ["Ai:Enabled"] = "true"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

        var geminiResponseJson = """
        {
            "candidates": [
                {
                    "content": {
                        "parts": [
                            {
                                "text": "Hello! I am your FinTrack financial assistant. How can I help you manage your money today?"
                            }
                        ]
                    }
                }
            ]
        }
        """;

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(geminiResponseJson, System.Text.Encoding.UTF8, "application/json")
        };

        var client = new HttpClient(new MockHttpMessageHandler(response));
        var service = new GeminiAiService(client, config, NullLogger<GeminiAiService>.Instance);

        var result = await service.ProcessTurnAsync("Hello assistant", [], []);

        result.IsSuccess.Should().BeTrue();
        result.ReplyText.Should().Contain("FinTrack financial assistant");
        result.FunctionCallName.Should().BeNull();
    }
}
