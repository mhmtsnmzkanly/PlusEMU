using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.Catalog.Utilities;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Pets.Horse;

internal class RemoveSaddleFromHorseEvent : IPacketEvent
{
    private readonly IRoomCreatureService _roomCreatureService;

    public RemoveSaddleFromHorseEvent(IRoomCreatureService roomCreatureService)
    {
        _roomCreatureService = roomCreatureService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _roomCreatureService.RemoveSaddleFromHorse(session, packet.ReadInt());
}
