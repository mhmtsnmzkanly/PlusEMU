using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Engine;

internal class MoveWallItemEvent : RoomPacketEvent
{
    private readonly IItemService _itemService;

    public MoveWallItemEvent(IItemService itemService)
    {
        _itemService = itemService;
    }


    public override async Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var itemId = packet.ReadUInt();
        var wallPositionData = packet.ReadString();

        var parts = wallPositionData.Split(':');
        if (parts.Length < 2) return;

        await _itemService.MoveWallItem(session, room, itemId, $":{parts[1]}");
    }
}
