using Plus.Communication.Packets.Outgoing.Sound;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users.Messenger.FriendBar;

namespace Plus.Communication.Packets.Incoming.Preferences;

internal class SetUIFlagsEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo)
            return Task.CompletedTask;

        habbo.FriendbarState = FriendBarStateUtility.GetEnum(packet.ReadInt());
        session.Send(new SoundSettingsComposer(habbo.ClientVolume, habbo.ChatPreference, habbo.AllowMessengerInvites, habbo.FocusPreference,
            FriendBarStateUtility.GetInt(habbo.FriendbarState)));
        return Task.CompletedTask;
    }
}
