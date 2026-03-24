using Plus.HabboHotel.GameClients;
using Plus.Utilities;
using System.Data;

namespace Plus.Communication.Packets.Outgoing.Marketplace;

public class MarketPlaceOwnOffersComposer : IServerPacket
{
    private readonly int _userId;
    public uint MessageId => ServerPacketHeader.MarketPlaceOwnOffersComposer;

    public MarketPlaceOwnOffersComposer(int userId)
    {
        _userId = userId;
    }

    public void Compose(IOutgoingPacket packet)
    {
        var i = 0;
        DataTable? table = null;
        using var dbClient = PlusEnvironment.DatabaseManager.GetQueryReactor();
        dbClient.SetQuery($"SELECT timestamp, state, offer_id, item_type, sprite_id, total_price, limited_number, limited_stack FROM catalog_marketplace_offers WHERE user_id = '{_userId}'");
        table = dbClient.GetTable();
        dbClient.SetQuery($"SELECT SUM(asking_price) FROM catalog_marketplace_offers WHERE state = '2' AND user_id = '{_userId}'");
        i = dbClient.GetInteger();
        packet.WriteInteger(i);
        if (table != null)
        {
            packet.WriteInteger(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                var num2 = Convert.ToInt32(Math.Floor(((double)row["timestamp"] + 172800.0 - UnixTimestamp.GetNow()) / 60.0));
                var num3 = int.Parse(Convert.ToString(row["state"]) ?? "0");
                if (num2 <= 0 && num3 != 2)
                {
                    num3 = 3;
                    num2 = 0;
                }
                packet.WriteInteger(Convert.ToInt32(row["offer_id"]));
                packet.WriteInteger(num3);
                packet.WriteInteger(1);
                packet.WriteInteger(Convert.ToInt32(row["sprite_id"]));
                packet.WriteInteger(256);
                packet.WriteString("");
                packet.WriteInteger(Convert.ToInt32(row["limited_number"]));
                packet.WriteInteger(Convert.ToInt32(row["limited_stack"]));
                packet.WriteInteger(Convert.ToInt32(row["total_price"]));
                packet.WriteInteger(num2);
                packet.WriteInteger(Convert.ToInt32(row["sprite_id"]));
            }
        }
        else
            packet.WriteInteger(0);
    }
}
