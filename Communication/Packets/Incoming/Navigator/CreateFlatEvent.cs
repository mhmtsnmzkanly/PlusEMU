using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Navigator;

namespace Plus.Communication.Packets.Incoming.Navigator;

internal class CreateFlatEvent : IPacketEvent
{
    private readonly INavigatorService _navigatorService;

    public CreateFlatEvent(INavigatorService navigatorService)
    {
        _navigatorService = navigatorService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var name = packet.ReadString();
        var description = packet.ReadString();
        var modelName = packet.ReadString();
        var category = packet.ReadInt();
        var maxVisitors = packet.ReadInt();
        var tradeSettings = packet.ReadInt();
        return _navigatorService.CreateFlat(session, name, description, modelName, category, maxVisitors, tradeSettings);
    }
}
