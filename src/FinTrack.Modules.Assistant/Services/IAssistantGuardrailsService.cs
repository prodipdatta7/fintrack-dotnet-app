using FinTrack.BuildingBlocks;

namespace FinTrack.Modules.Assistant.Services;

public interface IAssistantGuardrailsService
{
    Result<string> SanitizeAndValidateInput(string input);
    Result<bool> CheckRateLimit(string userId);
}
