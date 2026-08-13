using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Users.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Users.Features.UpdateSettings;

internal sealed class UpdateSettingsHandler : IRequestHandler<UpdateSettingsCommand, Result<UpdateSettingsResponse>>
{
    private readonly IMongoCollection<UserSettings> _userSettings;
    private readonly ICurrentUser _currentUser;

    public UpdateSettingsHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _userSettings = database.GetCollection<UserSettings>("user_settings");
        _currentUser = currentUser;
    }

    public async Task<Result<UpdateSettingsResponse>> Handle(
        UpdateSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await _userSettings
            .Find(s => s.UserId == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new UserSettings
            {
                UserId = _currentUser.UserId,
                CreatedBy = _currentUser.Email,
                CreatedAt = DateTime.UtcNow
            };
        }

        settings.Currency = request.Currency;
        settings.TimeZone = request.TimeZone;
        settings.DateFormat = request.DateFormat;
        settings.DefaultPageSize = request.DefaultPageSize;
        settings.EmailNotifications = request.EmailNotifications;
        settings.BudgetAlerts = request.BudgetAlerts;
        settings.BudgetAlertThreshold = request.BudgetAlertThreshold;
        settings.ModifiedAt = DateTime.UtcNow;

        await _userSettings.ReplaceOneAsync(
            s => s.UserId == _currentUser.UserId,
            settings,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken: cancellationToken);

        return Result<UpdateSettingsResponse>.Success(
            new UpdateSettingsResponse("Settings updated successfully."));
    }
}
