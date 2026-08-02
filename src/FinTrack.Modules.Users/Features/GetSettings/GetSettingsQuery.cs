using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Users.Features.GetSettings;

public sealed record GetSettingsQuery : IRequest<Result<GetSettingsResponse>>;

public sealed record GetSettingsResponse(
    string Currency,
    string TimeZone,
    string DateFormat,
    int DefaultPageSize,
    bool EmailNotifications,
    bool BudgetAlerts,
    decimal? BudgetAlertThreshold);
