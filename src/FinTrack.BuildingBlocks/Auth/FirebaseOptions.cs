using System.ComponentModel.DataAnnotations;

namespace FinTrack.BuildingBlocks.Auth;

/// <summary>
/// Strongly-typed configuration options for Firebase Authentication and Admin SDK integration.
/// </summary>
public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";

    [Required(ErrorMessage = "Firebase:ProjectId must be configured.")]
    public string ProjectId { get; set; } = string.Empty;

    public string? CredentialJson { get; set; }

    public string? CredentialPath { get; set; }
}
