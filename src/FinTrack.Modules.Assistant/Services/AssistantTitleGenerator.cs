using System.Text.RegularExpressions;

namespace FinTrack.Modules.Assistant.Services;

public static class AssistantTitleGenerator
{
    public static string GenerateTitle(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "New Conversation";

        var clean = Regex.Replace(input.Trim(), @"\s+", " ");
        clean = Regex.Replace(clean, @"[^\w\s\u0980-\u09FF\৳$€£,.-]", "");

        if (clean.Length <= 40)
            return CapitalizeFirst(clean);

        var words = clean.Split(' ');
        var titleWords = new List<string>();
        var currentLen = 0;

        foreach (var word in words)
        {
            if (currentLen + word.Length > 35 && titleWords.Count > 0)
                break;

            titleWords.Add(word);
            currentLen += word.Length + 1;
        }

        var result = string.Join(" ", titleWords);
        return CapitalizeFirst(result.Length > 40 ? result[..37] + "..." : result);
    }

    private static string CapitalizeFirst(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return s;

        return char.ToUpperInvariant(s[0]) + (s.Length > 1 ? s[1..] : "");
    }
}
