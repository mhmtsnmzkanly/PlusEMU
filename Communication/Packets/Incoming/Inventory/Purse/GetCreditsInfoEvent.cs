using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Inventory.Purse;

internal class GetCreditsInfoEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo)
            return Task.CompletedTask;

        session.Send(new CreditBalanceComposer(habbo.Credits));
        session.Send(new ActivityPointsComposer(habbo.Duckets, habbo.Diamonds, habbo.GotwPoints));
        return Task.CompletedTask;
    }
}
