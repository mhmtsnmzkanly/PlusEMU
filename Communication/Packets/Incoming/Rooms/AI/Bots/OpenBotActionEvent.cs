using Plus.Communication.Packets.Outgoing.Rooms.AI.Bots;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Bots;

internal class OpenBotActionEvent : IPacketEvent
{
    private readonly IRoomCreatureService _roomCreatureService;

    public OpenBotActionEvent(IRoomCreatureService roomCreatureService)
    {
        _roomCreatureService = roomCreatureService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _roomCreatureService.OpenBotAction(session, packet.ReadInt(), packet.ReadInt());
}
