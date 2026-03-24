using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.Communication.Packets.Outgoing.Groups;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.Core.Settings;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Chat.Filter;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class PurchaseGroupEvent : IPacketEvent
{
    private readonly IGroupService _groupService;

    public PurchaseGroupEvent(IGroupService groupService)
    {
        _groupService = groupService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var name = packet.ReadString();
        var description = packet.ReadString();
        var roomId = packet.ReadUInt();
        var mainColour = packet.ReadInt();
        var secondaryColour = packet.ReadInt();
        packet.ReadInt();
        var parts = new List<(int baseId, int firstPart, int secondPart)>(5);
        for (var i = 0; i < 5; i++)
            parts.Add((packet.ReadInt(), packet.ReadInt(), packet.ReadInt()));
        return _groupService.PurchaseGroup(session, name, description, roomId, mainColour, secondaryColour, parts);
    }
}
