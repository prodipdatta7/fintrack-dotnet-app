using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Users.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Users.Features.GetSettings;

internal sealed class GetSettingsHandler : IRequestHandler<GetSettingsQuery, Result<GetSettingsResponse>>
{
    private readonly IMongoCollection<UserSettings> _userSettings;
    private readonly ICurrentUser _currentUser;

    public GetSettingsHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _userSettings = database.GetCollection<UserSettings>("user_settings");
        _currentUser = currentUser;
    }

    public async Task<Result<GetSettingsResponse>> Handle(
        GetSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _userSettings
            .Find(s => s.UserId == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new UserSettings
            {
                UserId = _currentUser.UserId,
                Currency = "BDT",
                TimeZone = "Asia/Dhaka",
                DateFormat = "dd/MM/yyyy",
                DefaultPageSize = 10,
                EmailNotifications = true,
                BudgetAlerts = true,
                CreatedBy = _currentUser.Email,
                CreatedAt = DateTime.UtcNow
            };

            await _userSettings.InsertOneAsync(settings, cancellationToken: cancellationToken);
        }

        return Result<GetSettingsResponse>.Success(new GetSettingsResponse(
            settings.Currency,
            settings.TimeZone,
            settings.DateFormat,
            settings.DefaultPageSize,
            settings.EmailNotifications,
            settings.BudgetAlerts,
            settings.BudgetAlertThreshold));
    }
}
