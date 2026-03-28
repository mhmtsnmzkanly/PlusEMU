using Plus.HabboHotel.Items;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Instance;

internal sealed class WiredActorItemTriggerContext
{
    public WiredActorItemTriggerContext(Habbo actor, Item item)
    {
        Actor = actor;
        Item = item;
    }

    public Habbo Actor { get; }
    public Item Item { get; }
}
