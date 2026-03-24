using Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Pets;

internal class GetPetTrainingPanelEvent : IPacketEvent
{
    private readonly IRoomCreatureService _roomCreatureService;

    public GetPetTrainingPanelEvent(IRoomCreatureService roomCreatureService)
    {
        _roomCreatureService = roomCreatureService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _roomCreatureService.GetPetTrainingPanel(session, packet.ReadInt());
}
