using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Plus.Database;
using Plus.HabboHotel.Users.UserData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plus.HabboHotel.Users.Calendar;

internal class LoadUserCalendarTask : IUserDataLoadingTask
{
    private readonly IDatabase _database;
    private readonly ILogger<LoadUserCalendarTask> _logger;

    public LoadUserCalendarTask(IDatabase database, ILogger<LoadUserCalendarTask> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task Load(Habbo habbo)
    {
        var lateBoxes = new List<int>();
        var openedBoxes = new List<int>();

        try
        {
            using var connection = _database.Connection();
            var getData = await connection.QueryAsync<CalendarDayRow>(
                "SELECT `day` AS Day, `status` AS Status FROM `user_xmas15_calendar` WHERE `user_id` = @id;",
                new { id = habbo.Id });

            foreach (var row in getData)
            {
                if (row.Status == 0)
                    lateBoxes.Add(row.Day);
                else
                    openedBoxes.Add(row.Day);
            }
        }
        catch (MySqlException e) when (e.Message.Contains("user_xmas15_calendar"))
        {
            _logger.LogWarning("Skipping user calendar load because table user_xmas15_calendar is missing.");
        }

        habbo.Calendar = new(lateBoxes, openedBoxes);
    }

    private sealed class CalendarDayRow
    {
        public int Day { get; init; }
        public int Status { get; init; }
    }
}
