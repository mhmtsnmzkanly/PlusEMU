using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Bots;

internal class PlaceBotEvent : RoomPacketEvent
{
    private readonly IRoomCreatureService _roomCreatureService;

    public PlaceBotEvent(IRoomCreatureService roomCreatureService)
    {
        _roomCreatureService = roomCreatureService;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet) => _roomCreatureService.PlaceBot(room, session, packet.ReadInt(), packet.ReadInt(), packet.ReadInt());
}
