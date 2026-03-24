using Plus.Communication.Packets.Outgoing.Inventory.Pets;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Inventory.Pets;

internal class GetPetInventoryEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var inventory = session.GetHabbo().Inventory;
        if (inventory?.Pets == null)
            return Task.CompletedTask;
        var pets = inventory.Pets.Pets.Values.ToList();
        session.Send(new PetInventoryComposer(pets));
        return Task.CompletedTask;
    }
}
