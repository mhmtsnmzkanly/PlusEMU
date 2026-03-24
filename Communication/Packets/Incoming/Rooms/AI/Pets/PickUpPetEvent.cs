using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Pets;

internal class PickUpPetEvent : RoomPacketEvent
{
    private readonly IRoomCreatureService _roomCreatureService;

    public PickUpPetEvent(IRoomCreatureService roomCreatureService)
    {
        _roomCreatureService = roomCreatureService;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet) => _roomCreatureService.PickUpPet(room, session, packet.ReadInt());
}
