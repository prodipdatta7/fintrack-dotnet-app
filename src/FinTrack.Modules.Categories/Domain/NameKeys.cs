namespace FinTrack.Modules.Categories.Domain;

/// <summary>
/// Produces a stable, case-insensitive uniqueness key for user-facing names
/// (categories and tags). E.g. "  #Travel  " -> "travel".
/// </summary>
public static class NameKeys
{
    public static string Normalize(string name) =>
        name.Trim().TrimStart('#').ToLowerInvariant();
}