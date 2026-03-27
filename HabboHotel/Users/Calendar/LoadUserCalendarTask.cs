using Dapper;
using Plus.Database;
using Plus.HabboHotel.Users.UserData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plus.HabboHotel.Users.Calendar;

internal class LoadUserCalendarTask : IUserDataLoadingTask
{
    private readonly IDatabase _database;

    public LoadUserCalendarTask(IDatabase database)
    {
        _database = database;
    }

    public async Task Load(Habbo habbo)
    {
        using var connection = _database.Connection();
        var getData = await connection.QueryAsync<CalendarDayRow>(
            "SELECT `day` AS Day, `status` AS Status FROM `user_xmas15_calendar` WHERE `user_id` = @id;",
            new { id = habbo.Id });

        var lateBoxes = new List<int>();
        var openedBoxes = new List<int>();

        foreach (var row in getData)
        {
            if (row.Status == 0)
                lateBoxes.Add(row.Day);
            else
                openedBoxes.Add(row.Day);
        }

        habbo.Calendar = new(lateBoxes, openedBoxes);
    }

    private sealed class CalendarDayRow
    {
        public int Day { get; init; }
        public int Status { get; init; }
    }
}
