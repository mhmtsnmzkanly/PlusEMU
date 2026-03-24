using Plus.Communication.Packets;
using Plus.Communication.Packets.Outgoing.Inventory.Trading;

namespace Plus.HabboHotel.Rooms.Trading;

public sealed class Trade
{
    private readonly Room _instance;

    public Trade(int id, RoomUser playerOne, RoomUser playerTwo, Room room)
    {
        Id = id;
        CanChange = true;
        _instance = room;
        Users = new TradeUser[2];
        Users[0] = new(playerOne);
        Users[1] = new(playerTwo);
        playerOne.IsTrading = true;
        playerOne.TradeId = Id;
        playerOne.TradePartner = playerTwo.UserId;
        playerTwo.IsTrading = true;
        playerTwo.TradeId = Id;
        playerTwo.TradePartner = playerOne.UserId;
    }

    public int Id { get; set; }
    public TradeUser[] Users { get; set; }
    public bool CanChange { get; set; }

    public bool AllAccepted
    {
        get
        {
            foreach (var user in Users)
            {
                if (user == null)
                    continue;
                if (!user.HasAccepted) return false;
            }
            return true;
        }
    }

    public void SendPacket(IServerPacket packet)
    {
        foreach (var user in Users)
        {
            if (user == null || user.RoomUser == null || user.RoomUser.GetClient() == null)
                continue;
            user.RoomUser.GetClient().Send(packet);
        }
    }

    public void RemoveAccepted()
    {
        foreach (var user in Users)
        {
            if (user == null)
                continue;
            user.HasAccepted = false;
        }
    }

    public void EndTrade(int userId)
    {
        foreach (var tradeUser in Users)
        {
            if (tradeUser == null || tradeUser.RoomUser == null)
                continue;
            RemoveTrade(tradeUser.RoomUser.UserId);
        }
        SendPacket(new TradingClosedComposer(userId));
        _instance.GetTrading().RemoveTrade(Id);
    }

    public void RemoveTrade(int userId)
    {
        var tradeUser = Users[0];
        if (tradeUser.RoomUser.UserId != userId) tradeUser = Users[1];
        tradeUser.RoomUser.RemoveStatus("trd");
        tradeUser.RoomUser.UpdateNeeded = true;
        tradeUser.RoomUser.IsTrading = false;
        tradeUser.RoomUser.TradeId = 0;
        tradeUser.RoomUser.TradePartner = 0;
    }
}
