using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms;

public class RoomItemRemovalService : IRoomItemRemovalService
{
    public void PrepareItemRemoval(Room room, GameClient? session, Item item)
    {
        if (item.Definition.InteractionType == InteractionType.FootballGate)
            room.GetSoccer().UnRegisterGate(item);

        if (item.Definition.InteractionType != InteractionType.Gift)
            item.Interactor.OnRemove(session!, item);

        if (item.Definition.InteractionType == InteractionType.GuildGate)
        {
            item.UpdateCounter = 0;
            item.UpdateNeeded = false;
        }
    }

    public void BroadcastItemRemoval(Room room, Item item)
    {
        if (item.IsFloorItem)
            room.SendPacket(new ObjectRemoveComposer(item, item.UserId));
        else if (item.IsWallItem)
            room.SendPacket(new ItemRemoveComposer(item, item.UserId));
    }
}
