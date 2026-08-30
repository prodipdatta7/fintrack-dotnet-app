using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using FinTrack.BuildingBlocks;

namespace FinTrack.Modules.Assistant.Services;

public sealed class AssistantGuardrailsService : IAssistantGuardrailsService
{
    private static readonly ConcurrentDictionary<string, List<DateTime>> _rateLimitStore = new();
    private const int MaxRequestsPerMinute = 60;
    private const int MaxInputLength = 4000;

    private static readonly string[] _injectionPatterns =
    [
        @"ignore\s+(?:all\s+)?(?:previous|prior|above)?\s*instructions",
        @"disregard\s+(?:all\s+)?(?:previous|prior|above)?\s*rules",
        @"you\s+are\s+now\s+in\s+developer\s+mode",
        @"bypass\s+(?:all|the)?\s*restrictions",
        @"system\s*:\s*override",
        @"jailbreak",
        @"<script[^>]*>",
        @"javascript:"
    ];

    public Result<string> SanitizeAndValidateInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Result<string>.Failure("Input cannot be empty.");
        }

        var trimmed = input.Trim();

        if (trimmed.Length > MaxInputLength)
        {
            return Result<string>.Failure($"Input length exceeds maximum allowed limit of {MaxInputLength} characters.");
        }

        // 1. Prompt Injection Defense
        foreach (var pattern in _injectionPatterns)
        {
            if (Regex.IsMatch(trimmed, pattern, RegexOptions.IgnoreCase))
            {
                return Result<string>.Failure("Input contains prohibited or restricted instruction tokens.");
            }
        }

        // 2. Strip dangerous control characters (except tab, newline, carriage return)
        var sanitized = Regex.Replace(trimmed, @"[\u0000-\u0008\u000B\u000C\u000E-\u001F]", string.Empty);

        return Result<string>.Success(sanitized);
    }

    public Result<bool> CheckRateLimit(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<bool>.Success(true);
        }

        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-1);

        var timestamps = _rateLimitStore.GetOrAdd(userId, _ => new List<DateTime>());

        lock (timestamps)
        {
            timestamps.RemoveAll(ts => ts < windowStart);

            if (timestamps.Count >= MaxRequestsPerMinute)
            {
                return Result<bool>.Failure("Rate limit exceeded. Please wait a moment before sending more messages.");
            }

            timestamps.Add(now);
        }

        return Result<bool>.Success(true);
    }
}
