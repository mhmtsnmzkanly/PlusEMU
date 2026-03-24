using Plus.HabboHotel.GameClients;
using Plus.Utilities;

namespace Plus.Communication.Packets.Outgoing.Moderation;

public class ModeratorUserInfoComposer : IServerPacket
{
    private readonly object _user;
    private readonly object _info;
    private readonly bool _isOnline;
    public uint MessageId => ServerPacketHeader.ModeratorUserInfoComposer;

    public ModeratorUserInfoComposer(object user, object info, bool isOnline)
    {
        _user = user;
        _info = info;
        _isOnline = isOnline;
    }

    public void Compose(IOutgoingPacket packet)
    {
        var user = (dynamic)_user;
        var info = (dynamic)_info;
        var tradingLocked = info != null ? Convert.ToDouble(info.TradingLocked) : 0;
        var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(tradingLocked);
        packet.WriteInteger(user != null ? Convert.ToInt32(user.Id) : 0);
        packet.WriteString(user != null ? Convert.ToString(user.Username) ?? "Unknown" : "Unknown");
        packet.WriteString(user != null ? Convert.ToString(user.Look) ?? "Unknown" : "Unknown");
        packet.WriteInteger(user != null ? Convert.ToInt32(Math.Ceiling((UnixTimestamp.GetNow() - Convert.ToDouble(user.AccountCreated)) / 60)) : 0);
        packet.WriteInteger(user != null ? Convert.ToInt32(Math.Ceiling((UnixTimestamp.GetNow() - Convert.ToDouble(user.LastOnline)) / 60)) : 0);
        packet.WriteBoolean(_isOnline);
        packet.WriteInteger(info != null ? Convert.ToInt32(info.Cfhs) : 0);
        packet.WriteInteger(info != null ? Convert.ToInt32(info.CfhsAbusive) : 0);
        packet.WriteInteger(info != null ? Convert.ToInt32(info.Cautions) : 0);
        packet.WriteInteger(info != null ? Convert.ToInt32(info.Bans) : 0);
        packet.WriteInteger(info != null ? Convert.ToInt32(info.TradingLocksCount) : 0); //Trading lock counts
        packet.WriteString(tradingLocked != 0 ? origin.ToString("dd/MM/yyyy HH:mm:ss") : "0"); //Trading lock
        packet.WriteString(""); //Purchases
        packet.WriteInteger(0); //Itendity information tool
        packet.WriteInteger(0); //Id bans.
        packet.WriteString(user != null ? Convert.ToString(user.Mail) ?? "Unknown" : "Unknown");
        packet.WriteString(""); //user_classification
    }
}
