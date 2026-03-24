using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Trading;

namespace Plus.Communication.Packets.Incoming.Inventory.Trading;

internal class TradingCancelConfirmEvent : IPacketEvent
{
    private readonly ITradingService _tradingService;

    public TradingCancelConfirmEvent(ITradingService tradingService)
    {
        _tradingService = tradingService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _tradingService.CancelConfirm(session);
}
