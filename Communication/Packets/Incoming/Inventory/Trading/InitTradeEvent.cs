using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Trading;

namespace Plus.Communication.Packets.Incoming.Inventory.Trading;

internal class InitTradeEvent : IPacketEvent
{
    private readonly ITradingService _tradingService;

    public InitTradeEvent(ITradingService tradingService)
    {
        _tradingService = tradingService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
        => _tradingService.StartTrade(session, packet.ReadInt());
}
