using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Trading;

namespace Plus.Communication.Packets.Incoming.Inventory.Trading;

internal class TradingRemoveItemEvent : IPacketEvent
{
    private readonly ITradingService _tradingService;

    public TradingRemoveItemEvent(ITradingService tradingService)
    {
        _tradingService = tradingService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _tradingService.RemoveItem(session, packet.ReadUInt());
}
