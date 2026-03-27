using Dapper;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.Utilities;

namespace Plus.Communication.Packets.Outgoing.Marketplace;

public class MarketPlaceOwnOffersComposer : IServerPacket
{
    private readonly int _userId;
    private readonly IDatabase _database;
    public uint MessageId => ServerPacketHeader.MarketPlaceOwnOffersComposer;

    public MarketPlaceOwnOffersComposer(int userId, IDatabase database)
    {
        _userId = userId;
        _database = database;
    }

    public void Compose(IOutgoingPacket packet)
    {
        using var db = _database.Connection();
        var rows = db.Query(
            "SELECT `timestamp`, `state`, `offer_id`, `item_type`, `sprite_id`, `total_price`, `limited_number`, `limited_stack` FROM `catalog_marketplace_offers` WHERE `user_id` = @userId",
            new { userId = _userId }).AsList();
        var pendingCredits = db.QueryFirstOrDefault<int>(
            "SELECT SUM(`asking_price`) FROM `catalog_marketplace_offers` WHERE `state` = '2' AND `user_id` = @userId",
            new { userId = _userId });
        packet.WriteInteger(pendingCredits);
        packet.WriteInteger(rows.Count);
        foreach (var row in rows)
        {
            var num2 = Convert.ToInt32(Math.Floor(((double)row.timestamp + 172800.0 - UnixTimestamp.GetNow()) / 60.0));
            var num3 = int.Parse(((string?)row.state) ?? "0");
            if (num2 <= 0 && num3 != 2)
            {
                num3 = 3;
                num2 = 0;
            }
            packet.WriteInteger((int)row.offer_id);
            packet.WriteInteger(num3);
            packet.WriteInteger(1);
            packet.WriteInteger((int)row.sprite_id);
            packet.WriteInteger(256);
            packet.WriteString("");
            packet.WriteInteger((int)row.limited_number);
            packet.WriteInteger((int)row.limited_stack);
            packet.WriteInteger((int)row.total_price);
            packet.WriteInteger(num2);
            packet.WriteInteger((int)row.sprite_id);
        }
    }
}
