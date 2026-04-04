using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Plus.Core.Settings;
using Plus.Database;
using Plus.Communication.Packets.Outgoing.Campaign;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Users.Calendar;

internal sealed class SeasonalCalendarService : ISeasonalCalendarService
{
    private const string DefaultCampaignName = "xmas15";
    private const string DefaultCampaignImage = "xmas15";
    private const int DefaultTotalDays = 24;

    private readonly IDatabase _database;
    private readonly ISettingsManager _settingsManager;
    private readonly ILogger<SeasonalCalendarService> _logger;

    public SeasonalCalendarService(IDatabase database, ISettingsManager settingsManager, ILogger<SeasonalCalendarService> logger)
    {
        _database = database;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    public Task SendCalendarData(GameClient session)
    {
        if (!_settingsManager.GetBoolOrDefault("hotel.calendar.enabled", true))
            return Task.CompletedTask;

        if (session.GetHabbo() is not { Calendar: { } calendar })
            return Task.CompletedTask;

        session.Send(new SeasonalCalendarDataComposer(
            _settingsManager.GetStringOrDefault("hotel.calendar.default", DefaultCampaignName),
            _settingsManager.GetStringOrDefault("hotel.calendar.image", DefaultCampaignImage),
            GetCurrentCampaignDay(),
            _settingsManager.GetIntOrDefault("hotel.calendar.total_days", DefaultTotalDays),
            calendar.GetOpenedBoxes(),
            calendar.GetLateBoxes()));

        return Task.CompletedTask;
    }

    public async Task OpenDoor(GameClient session, string campaignName, int day, bool force)
    {
        if (!_settingsManager.GetBoolOrDefault("hotel.calendar.enabled", true))
            return;

        if (session.GetHabbo() is not { } habbo)
            return;

        habbo.Calendar ??= new(new List<int>(), new List<int>());

        if (day < 1 || day > _settingsManager.GetIntOrDefault("hotel.calendar.total_days", DefaultTotalDays))
            return;

        var currentDay = GetCurrentCampaignDay();
        if (!force && day > currentDay)
            return;

        var openedBoxes = habbo.Calendar.GetOpenedBoxes();
        var lateBoxes = habbo.Calendar.GetLateBoxes();

        if (!openedBoxes.Contains(day))
            openedBoxes.Add(day);
        lateBoxes.Remove(day);

        try
        {
            using var connection = _database.Connection();
            await connection.ExecuteAsync(
                """
                INSERT INTO `user_xmas15_calendar` (`user_id`, `day`, `status`)
                VALUES (@userId, @day, 1)
                ON DUPLICATE KEY UPDATE `status` = VALUES(`status`);
                """,
                new { userId = habbo.Id, day });
        }
        catch (MySqlException e) when (e.Message.Contains("user_xmas15_calendar"))
        {
            _logger.LogWarning("Skipping calendar door persistence because table user_xmas15_calendar is missing.");
        }

        await SendCalendarData(session);
    }

    private static int GetCurrentCampaignDay()
    {
        var now = DateTime.UtcNow;
        return Math.Clamp(now.Day, 1, DefaultTotalDays);
    }
}
