using Dapper;
using Plus.Database;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Users.Inventory.Bots;

namespace Plus.HabboHotel.Catalog.Utilities;

public class BotUtility : IBotUtility
{
    private readonly IDatabase _database;
    private readonly ICatalogManager _catalogManager;

    public BotUtility(IDatabase database, ICatalogManager catalogManager)
    {
        _database = database;
        _catalogManager = catalogManager;
    }

    public Bot? CreateBot(ItemDefinition itemDefinition, int ownerId)
    {
        if (!_catalogManager.TryGetBot(itemDefinition.Id, out var cataBot) || cataBot == null)
            return null;

        using var db = _database.Connection();
        var newId = db.ExecuteScalar<int>(
            "INSERT INTO `bots` (`user_id`, `name`, `motto`, `look`, `gender`, `ai_type`) VALUES (@ownerId, @name, @motto, @figure, @gender, @aiType); SELECT LAST_INSERT_ID();",
            new { ownerId, name = cataBot.Name, motto = cataBot.Motto, figure = cataBot.Figure, gender = cataBot.Gender, aiType = cataBot.AiType });
        
        var botData = db.QueryFirstOrDefault(
            "SELECT `id`, `user_id`, `name`, `motto`, `look`, `gender` FROM `bots` WHERE `user_id` = @ownerId AND `id` = @id LIMIT 1",
            new { ownerId, id = newId });
        
        if (botData == null)
            return null;

        return new(
            (int)botData.id,
            (int)botData.user_id,
            ((string?)botData.name) ?? string.Empty,
            ((string?)botData.motto) ?? string.Empty,
            ((string?)botData.look) ?? string.Empty,
            ((string?)botData.gender) ?? string.Empty);
    }

    public BotAiType GetAiFromString(string type) => GetAiTypeFromString(type);

    public static BotAiType GetAiTypeFromString(string type)
    {
        return type switch
        {
            "pet" => BotAiType.Pet,
            "generic" => BotAiType.Generic,
            "bartender" => BotAiType.Bartender,
            _ => BotAiType.Generic
        };
    }
}
