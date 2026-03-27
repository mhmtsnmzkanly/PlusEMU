using Dapper;
using Plus.Database;

namespace Plus.HabboHotel.Users.Inventory.Bots;

internal class BotLoader : IBotLoader
{
    private readonly IDatabase _database;

    public BotLoader(IDatabase database)
    {
        _database = database;
    }

    public List<Bot> GetBotsForUser(int userId)
    {
        using var db = _database.Connection();
        var rows = db.Query(
            "SELECT `id`, `user_id`, `name`, `motto`, `look`, `gender` FROM `bots` WHERE `user_id` = @userId AND `room_id` = '0' AND `ai_type` != 'pet'",
            new { userId });
        var bots = new List<Bot>();
        foreach (var row in rows)
        {
            bots.Add(new(
                (int)row.id,
                (int)row.user_id,
                ((string?)row.name) ?? string.Empty,
                ((string?)row.motto) ?? string.Empty,
                ((string?)row.look) ?? string.Empty,
                ((string?)row.gender) ?? string.Empty));
        }
        return bots;
    }
}
