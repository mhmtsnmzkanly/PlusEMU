using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Bots;

internal class PickUpBotEvent : IPacketEvent
{
    private readonly IRoomCreatureService _roomCreatureService;

    public PickUpBotEvent(IRoomCreatureService roomCreatureService)
    {
        _roomCreatureService = roomCreatureService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _roomCreatureService.PickUpBot(session, packet.ReadInt());
}
