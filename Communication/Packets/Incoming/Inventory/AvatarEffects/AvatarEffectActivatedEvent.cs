using Plus.Communication.Packets.Outgoing.Inventory.AvatarEffects;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Inventory.AvatarEffects;

internal class AvatarEffectActivatedEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var effects = habbo?.Effects;
        var effectId = packet.ReadInt();
        if (effects == null)
            return Task.CompletedTask;
        var effect = effects.GetEffectNullable(effectId, false, true);
        if (effects.HasEffect(effectId, true) || effect == null) return Task.CompletedTask;
        if (effect.Activate()) session.Send(new AvatarEffectActivatedComposer(effect));
        return Task.CompletedTask;
    }
}
