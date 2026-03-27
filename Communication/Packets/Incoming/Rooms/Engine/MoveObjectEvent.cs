using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Engine;

internal class MoveObjectEvent : RoomPacketEvent
{
    private readonly IRoomManager _roomManager;
    private readonly IQuestService _questService;

    public MoveObjectEvent(IRoomManager roomManager, IQuestService questService)
    {
        _roomManager = roomManager;
        _questService = questService;
    }

    public override async Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var itemId = packet.ReadUInt();
        if (itemId == 0)
            return;
        Item item;
        if (room.Group != null)
        {
            if (!room.CheckRights(session, false, true))
            {
                item = room.GetRoomItemHandler().GetItem(itemId);
                if (item == null)
                    return;
                session.Send(new ObjectUpdateComposer(item));
                return;
            }
        }
        else
        {
            if (!room.CheckRights(session)) return;
        }
        item = room.GetRoomItemHandler().GetItem(itemId);
        if (item == null)
            return;
        var x = packet.ReadInt();
        var y = packet.ReadInt();
        var rotation = packet.ReadInt();
        
        if (x != item.GetX || y != item.GetY)
            await _questService.ProgressUserQuest(session, QuestType.FurniMove);
        if (rotation != item.Rotation)
            await _questService.ProgressUserQuest(session, QuestType.FurniRotate);
            
        if (!room.GetRoomItemHandler().SetFloorItem(session, item, x, y, rotation, false, false, true))
        {
            room.SendPacket(new ObjectUpdateComposer(item));
            return;
        }
        if (item.GetZ >= 0.1)
            await _questService.ProgressUserQuest(session, QuestType.FurniStack);
    }
}