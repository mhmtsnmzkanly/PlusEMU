using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Users.Inventory.Bots;

namespace Plus.HabboHotel.Catalog.Utilities;

public interface IBotUtility
{
    Bot? CreateBot(ItemDefinition itemDefinition, int ownerId);
    BotAiType GetAiFromString(string type);
}
