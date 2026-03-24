using Plus.Communication.Packets.Outgoing.Inventory.Bots;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Inventory.Bots;

internal class GetBotInventoryEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var inventory = habbo?.Inventory;
        if (inventory?.Bots == null)
            return Task.CompletedTask;
        var bots = inventory.Bots.Bots.Values.ToList();
        session.Send(new BotInventoryComposer(bots));
        return Task.CompletedTask;
    }
}
