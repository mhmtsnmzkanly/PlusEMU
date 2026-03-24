using Plus.Communication.Packets.Outgoing.Pets;
using Plus.Communication.Packets.Outgoing.Rooms.Avatar;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Pets;

internal class RespectPetEvent : RoomPacketEvent
{
    private readonly IRoomCreatureService _roomCreatureService;

    public RespectPetEvent(IRoomCreatureService roomCreatureService)
    {
        _roomCreatureService = roomCreatureService;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet) => _roomCreatureService.RespectPet(room, session, packet.ReadInt());
}
