using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Trading;

public interface ITradingService
{
    Task StartTrade(GameClient session, int targetVirtualId);
    Task OfferItem(GameClient session, uint itemId);
    Task OfferItems(GameClient session, uint itemId, int amount);
    Task RemoveItem(GameClient session, uint itemId);
    Task Accept(GameClient session);
    Task Confirm(GameClient session);
    Task Cancel(GameClient session);
    Task CancelConfirm(GameClient session);
    Task Modify(GameClient session);
}
