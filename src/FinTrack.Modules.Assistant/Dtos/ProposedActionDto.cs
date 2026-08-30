namespace FinTrack.Modules.Assistant.Dtos;

public sealed record ProposedActionDto<TPayload>(
    string ActionType,
    string Status,
    string Summary,
    TPayload Payload)
{
    public const string ProposedStatus = "Proposed";

    public static ProposedActionDto<TPayload> Create(string actionType, string summary, TPayload payload) =>
        new(actionType, ProposedStatus, summary, payload);
}
