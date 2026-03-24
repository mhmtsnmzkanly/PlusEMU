using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Trading;

namespace Plus.Communication.Packets.Incoming.Inventory.Trading;

internal class TradingOfferItemsEvent : IPacketEvent
{
    private readonly ITradingService _tradingService;

    public TradingOfferItemsEvent(ITradingService tradingService)
    {
        _tradingService = tradingService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var amount = packet.ReadInt();
        var itemId = packet.ReadUInt();
        return _tradingService.OfferItems(session, itemId, amount);
    }
}
