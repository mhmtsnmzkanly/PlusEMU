using Dapper;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Users.Inventory.Bots;

namespace Plus.HabboHotel.Catalog.Utilities;

public static class BotUtility
{
    public static Bot? CreateBot(ItemDefinition itemDefinition, int ownerId)
    {
        if (!PlusEnvironment.Game.Catalog.TryGetBot(itemDefinition.Id, out var cataBot))
            return null;
        using var db = PlusEnvironment.DatabaseManager.Connection();
        var newId = db.ExecuteScalar<int>(
            "INSERT INTO `bots` (`user_id`, `name`, `motto`, `look`, `gender`, `ai_type`) VALUES (@ownerId, @name, @motto, @figure, @gender, @aiType); SELECT LAST_INSERT_ID();",
            new { ownerId, name = cataBot.Name, motto = cataBot.Motto, figure = cataBot.Figure, gender = cataBot.Gender, aiType = cataBot.AiType });
        var bot = db.QueryFirstOrDefault(
            "SELECT `id`, `user_id`, `name`, `motto`, `look`, `gender` FROM `bots` WHERE `user_id` = @ownerId AND `id` = @id LIMIT 1",
            new { ownerId, id = newId });
        if (bot == null)
            return null;
        return new(
            (int)bot.id,
            (int)bot.user_id,
            ((string?)bot.name) ?? string.Empty,
            ((string?)bot.motto) ?? string.Empty,
            ((string?)bot.look) ?? string.Empty,
            ((string?)bot.gender) ?? string.Empty);
    }

    public static BotAiType GetAiFromString(string type)
    {
        switch (type)
        {
            case "pet":
                return BotAiType.Pet;
            case "generic":
                return BotAiType.Generic;
            case "bartender":
                return BotAiType.Bartender;
            default:
                return BotAiType.Generic;
        }
    }
}
