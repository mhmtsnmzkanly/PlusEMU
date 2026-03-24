using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.Communication.Packets.Incoming.Inventory.Furni;

internal class RequestFurniInventoryEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var furniture = habbo?.Inventory?.Furniture;
        if (furniture == null)
        {
            session.Send(new FurniListComposer(new List<InventoryItem>(), 1, 1));
            return Task.CompletedTask;
        }

        var items = furniture.AllItems.ToList();
        var page = 0;
        var pages = (items.Count - 1) / 700 + 1;
        if (!items.Any())
            session.Send(new FurniListComposer(items.ToList(), 1, 1));
        else
        {
            foreach (ICollection<InventoryItem> batch in items.Chunk(700))
            {
                session.Send(new FurniListComposer(batch.ToList(), pages, page));
                page++;
            }
        }
        return Task.CompletedTask;
    }
}
