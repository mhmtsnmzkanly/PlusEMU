using Plus.Communication.Packets.Outgoing.Inventory.Trading;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Trading;

namespace Plus.Communication.Packets.Incoming.Inventory.Trading;

internal class TradingCancelEvent : IPacketEvent
{
    private readonly ITradingService _tradingService;

    public TradingCancelEvent(ITradingService tradingService)
    {
        _tradingService = tradingService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _tradingService.Cancel(session);
}
