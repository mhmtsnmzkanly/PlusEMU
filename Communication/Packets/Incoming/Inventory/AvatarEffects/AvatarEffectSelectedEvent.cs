using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Inventory.AvatarEffects;

internal class AvatarEffectSelectedEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { Effects: { } effects } habbo)
            return Task.CompletedTask;

        var effectId = packet.ReadInt();
        if (effectId < 0)
            effectId = 0;
        if (!habbo.TryGetCurrentRoom(out var room))
            return Task.CompletedTask;

        if (!room.GetRoomUserManager().TryGetRoomUserByHabbo(habbo.Id, out var user) || user == null)
            return Task.CompletedTask;
        if (effectId != 0 && effects.HasEffect(effectId, true))
            user.ApplyEffect(effectId);

        return Task.CompletedTask;
    }
}
