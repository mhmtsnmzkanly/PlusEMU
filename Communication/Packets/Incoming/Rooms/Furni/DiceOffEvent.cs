using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni;

internal class DiceOffEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || !habbo.TryGetCurrentRoom(out var room))
            return Task.CompletedTask;
        var item = room.GetRoomItemHandler().GetItem(packet.ReadUInt());
        if (item == null)
            return Task.CompletedTask;
        var hasRights = room.CheckRights(session);
        item.Interactor.OnTrigger(session, item, -1, hasRights);
        return Task.CompletedTask;
    }
}
