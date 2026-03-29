using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;

namespace Plus.Communication.Packets.Outgoing.Rooms.Engine;

public class ItemUpdateComposer : IServerPacket
{
    private readonly Item _item;
    public uint MessageId => ServerPacketHeader.ItemUpdateComposer;

    public ItemUpdateComposer(Item item)
    {
        _item = item;
    }

    public void Compose(IOutgoingPacket packet)
    {
        WriteWallItem(packet, _item);
    }

    private void WriteWallItem(IOutgoingPacket packet, Item item)
    {
        packet.WriteString(item.Id.ToString());
        packet.WriteInteger(item.Definition.SpriteId);
        packet.WriteString(item.WallCoordinates);
        packet.WriteString(item.GetWallExtradataValue());
        packet.WriteInteger(-1);
        packet.WriteInteger(item.Definition.Modes > 1 ? 1 : 0);
        packet.WriteUInt(item.OwnerId);
    }
}
