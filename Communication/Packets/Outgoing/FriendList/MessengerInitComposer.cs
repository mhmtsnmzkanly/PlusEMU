using Plus.Core.Settings;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.FriendList;

public class MessengerInitComposer : IServerPacket
{
    private readonly ISettingsManager _settingsManager;
    public uint MessageId => ServerPacketHeader.MessengerInitComposer;

    public MessengerInitComposer(ISettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_settingsManager.GetIntOrDefault("messenger.buddy_limit", 0)); //Friends max.
        packet.WriteInteger(300);
        packet.WriteInteger(800);
        packet.WriteInteger(0); // category count
    }
}
