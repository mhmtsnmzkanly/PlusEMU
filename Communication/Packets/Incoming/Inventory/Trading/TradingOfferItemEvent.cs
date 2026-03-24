using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Trading;

namespace Plus.Communication.Packets.Incoming.Inventory.Trading;

internal class TradingOfferItemEvent : IPacketEvent
{
    private readonly ITradingService _tradingService;

    public TradingOfferItemEvent(ITradingService tradingService)
    {
        _tradingService = tradingService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _tradingService.OfferItem(session, packet.ReadUInt());
}
