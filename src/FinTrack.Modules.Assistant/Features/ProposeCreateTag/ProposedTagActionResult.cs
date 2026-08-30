namespace FinTrack.Modules.Assistant.Features.ProposeCreateTag;

public sealed record ProposedCreateTagPayload(
    string Name,
    string NormalizedName,
    bool AlreadyExists);
