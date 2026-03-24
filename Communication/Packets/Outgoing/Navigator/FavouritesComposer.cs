using System.Collections;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Navigator;

public class FavouritesComposer : IServerPacket
{
    private readonly ArrayList _favouriteIds;
    public uint MessageId => ServerPacketHeader.FavouritesComposer;

    public FavouritesComposer(ArrayList favouriteIds)
    {
        _favouriteIds = favouriteIds;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(50);
        packet.WriteInteger(_favouriteIds.Count);
        foreach (var favouriteId in _favouriteIds.ToArray())
        {
            if (favouriteId is int id)
                packet.WriteInteger(id);
        }
    }
}
