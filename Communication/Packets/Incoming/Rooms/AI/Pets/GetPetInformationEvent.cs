using Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Pets;

internal class GetPetInformationEvent : IPacketEvent
{
    private readonly IRoomCreatureService _roomCreatureService;

    public GetPetInformationEvent(IRoomCreatureService roomCreatureService)
    {
        _roomCreatureService = roomCreatureService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _roomCreatureService.GetPetInformation(session, packet.ReadInt());
}
