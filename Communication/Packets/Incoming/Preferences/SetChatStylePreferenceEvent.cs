using Plus.Database;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Preferences;

internal class SetChatStylePreferenceEvent : IPacketEvent
{
    private readonly IDatabase _database;

    public SetChatStylePreferenceEvent(IDatabase database)
    {
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo)
            return Task.CompletedTask;

        var chatBubbleId = packet.ReadInt();
        habbo.CustomBubbleId = chatBubbleId;
        habbo.SaveChatBubble(_database, chatBubbleId);
        return Task.CompletedTask;
    }
}
