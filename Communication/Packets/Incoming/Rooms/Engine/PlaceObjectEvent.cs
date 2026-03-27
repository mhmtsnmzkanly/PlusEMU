using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Rooms.Notifications;
using Plus.Core.Settings;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Engine;

internal class PlaceObjectEvent : RoomPacketEvent
{
    private readonly IItemService _itemService;

    public PlaceObjectEvent(IItemService itemService)
    {
        _itemService = itemService;
    }

    /// TODO @80O: Unfuck this mess
    public override async Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var furniture = habbo?.Inventory?.Furniture;
        if (habbo?.Permissions == null || furniture == null)
            return;

        var rawData = packet.ReadString();
        var data = rawData.Split(' ');
        if (!uint.TryParse(data[0], out var itemId))
            return;

        await _itemService.PlaceItem(session, room, itemId, rawData);
    }
}
