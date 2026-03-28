using Plus.Communication.Packets.Outgoing.Inventory.Bots;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Inventory.Bots;

internal class GetBotInventoryEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo()?.Inventory?.Bots is not { } botsInventory)
            return Task.CompletedTask;

        var bots = botsInventory.Bots.Values.ToList();
        session.Send(new BotInventoryComposer(bots));
        return Task.CompletedTask;
    }
}
