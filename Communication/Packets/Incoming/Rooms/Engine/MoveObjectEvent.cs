using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Engine;

internal class MoveObjectEvent : RoomPacketEvent
{
    private readonly IItemService _itemService;

    public MoveObjectEvent(IItemService itemService)
    {
        _itemService = itemService;
    }

    public override async Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var itemId = packet.ReadUInt();
        if (itemId == 0)
            return;

        var x = packet.ReadInt();
        var y = packet.ReadInt();
        var rotation = packet.ReadInt();

        await _itemService.MoveItem(session, room, itemId, x, y, rotation);
    }
}