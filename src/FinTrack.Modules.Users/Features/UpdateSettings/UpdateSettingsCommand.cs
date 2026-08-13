using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Users.Features.UpdateSettings;

public sealed record UpdateSettingsCommand(
    string Currency,
    string TimeZone,
    string DateFormat,
    int DefaultPageSize,
    bool EmailNotifications,
    bool BudgetAlerts,
    decimal? BudgetAlertThreshold) : IRequest<Result<UpdateSettingsResponse>>;

public sealed record UpdateSettingsResponse(string Message);
