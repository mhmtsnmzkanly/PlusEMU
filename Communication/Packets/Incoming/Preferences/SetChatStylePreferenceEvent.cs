using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Preferences;

internal class SetChatStylePreferenceEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        var chatBubbleId = packet.ReadInt();

        habbo.CustomBubbleId = chatBubbleId;
        habbo.SaveChatBubble(chatBubbleId.ToString());

        return Task.CompletedTask;
    }
}
