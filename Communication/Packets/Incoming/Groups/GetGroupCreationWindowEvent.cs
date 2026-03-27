using Plus.Communication.Packets.Outgoing.Groups;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class GetGroupCreationWindowEvent : IPacketEvent
{
    private readonly IRoomFactory _roomFactory;

    public GetGroupCreationWindowEvent(IRoomFactory roomFactory)
    {
        _roomFactory = roomFactory;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        var rooms = _roomFactory.GetRoomsDataByOwnerSortByName(habbo.Id).Where(x => x.Group == null).ToList();
        session.Send(new GroupCreationWindowComposer(rooms));
        return Task.CompletedTask;
    }
}
