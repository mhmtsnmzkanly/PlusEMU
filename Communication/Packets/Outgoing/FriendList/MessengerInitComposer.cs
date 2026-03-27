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
        packet.WriteInteger(Convert.ToInt32(_settingsManager.TryGetValue("messenger.buddy_limit"))); //Friends max.
        packet.WriteInteger(300);
        packet.WriteInteger(800);
        packet.WriteInteger(0); // category count
    }
}