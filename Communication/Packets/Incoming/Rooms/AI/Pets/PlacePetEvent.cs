using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Pets;

internal class PlacePetEvent : RoomPacketEvent
{
    private readonly IRoomCreatureService _roomCreatureService;

    public PlacePetEvent(IRoomCreatureService roomCreatureService)
    {
        _roomCreatureService = roomCreatureService;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet) => _roomCreatureService.PlacePet(room, session, packet.ReadInt(), packet.ReadInt(), packet.ReadInt());
}
