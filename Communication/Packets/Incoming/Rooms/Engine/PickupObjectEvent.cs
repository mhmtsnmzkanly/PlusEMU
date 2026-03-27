using Dapper;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Engine;

internal class PickupObjectEvent : RoomPacketEvent
{
    private readonly IItemService _itemService;

    public PickupObjectEvent(IItemService itemService)
    {
        _itemService = itemService;
    }

    public override async Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        packet.ReadInt(); //unknown
        var itemId = packet.ReadUInt();

        await _itemService.PickupItem(session, room, itemId);
    }
}
