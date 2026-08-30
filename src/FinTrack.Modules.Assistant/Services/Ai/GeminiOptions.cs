namespace FinTrack.Modules.Assistant.Services.Ai;

public sealed class GeminiOptions
{
    public const string SectionName = "Ai";

    public string? GeminiApiKey { get; set; }
    public string Model { get; set; } = "gemini-2.0-flash";
    public bool Enabled { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 10;
}
