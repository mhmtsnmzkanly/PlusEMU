using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.Trading;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.Communication.Packets.Outgoing.Inventory.Trading;

public class TradingUpdateComposer : IServerPacket
{
    private readonly Trade _trade;
    public uint MessageId => ServerPacketHeader.TradingUpdateComposer;

    public TradingUpdateComposer(Trade trade)
    {
        _trade = trade;
    }

    public void Compose(IOutgoingPacket packet)
    {
        foreach (var user in _trade.Users.ToList())
        {
            packet.WriteInteger(user.RoomUser.UserId);
            packet.WriteInteger(user.OfferedItems.Count);
            foreach (var item in user.OfferedItems.Values)
            {
                packet.WriteUInteger(item.Id);
                packet.WriteString(item.Definition.Type.ToString().ToLower());
                packet.WriteUInteger(item.Id);
                packet.WriteInteger(item.Definition.SpriteId);
                packet.WriteInteger(0); //Not sure.
                if (item.UniqueNumber > 0)
                {
                    packet.WriteBoolean(false); //Stackable
                    packet.WriteInteger(256);
                    packet.WriteString("");
                    packet.WriteUInteger(item.UniqueNumber);
                    packet.WriteUInteger(item.UniqueSeries);
                }
                else
                {
                    packet.WriteBoolean(true); //Stackable
                    packet.WriteInteger(0);
                    packet.WriteString("");
                }
                packet.WriteInteger(0);
                packet.WriteInteger(0);
                packet.WriteInteger(0);
                if (item.Definition.Type == ItemType.Floor)
                    packet.WriteInteger(0);
            }
            packet.WriteInteger(user.OfferedItems.Count); //Item Count
            packet.WriteInteger(user.OfferedItems.Values.Where(x => x.Definition.IsExchange).Sum(t => t.Definition.BehaviourData));
        }

    }
}
