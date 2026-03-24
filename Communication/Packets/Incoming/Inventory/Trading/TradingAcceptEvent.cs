using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Trading;

namespace Plus.Communication.Packets.Incoming.Inventory.Trading;

internal class TradingAcceptEvent : IPacketEvent
{
    private readonly ITradingService _tradingService;

    public TradingAcceptEvent(ITradingService tradingService)
    {
        _tradingService = tradingService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _tradingService.Accept(session);
}
