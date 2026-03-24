using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Settings;

internal class ModifyRoomFilterListEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || !habbo.InRoom)
            return Task.CompletedTask;
        var instance = habbo.CurrentRoom;
        if (instance == null)
            return Task.CompletedTask;
        if (!instance.CheckRights(session))
            return Task.CompletedTask;
        packet.ReadInt(); //roomId
        var added = packet.ReadBool();
        var word = packet.ReadString();
        if (added)
            instance.GetFilter().AddFilter(word);
        else
            instance.GetFilter().RemoveFilter(word);
        return Task.CompletedTask;
    }
}
