using Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Chat.Pets.Locale;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Pets.Horse;

internal class RideHorseEvent : RoomPacketEvent
{
    private readonly IRoomCreatureService _roomCreatureService;

    public RideHorseEvent(IRoomCreatureService roomCreatureService)
    {
        _roomCreatureService = roomCreatureService;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet) => _roomCreatureService.RideHorse(room, session, packet.ReadInt(), packet.ReadBool());
}
