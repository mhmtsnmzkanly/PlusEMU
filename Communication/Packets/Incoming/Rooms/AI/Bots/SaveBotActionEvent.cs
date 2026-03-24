using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Bots;

internal class SaveBotActionEvent : IPacketEvent
{
    private readonly IRoomCreatureService _roomCreatureService;

    public SaveBotActionEvent(IRoomCreatureService roomCreatureService)
    {
        _roomCreatureService = roomCreatureService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
        => _roomCreatureService.SaveBotAction(session, packet.ReadInt(), packet.ReadInt(), packet.ReadString());
}
