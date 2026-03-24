using Plus.Communication.Packets.Outgoing.FriendList;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class FollowFriendEvent : IPacketEvent
{
    private readonly IGameClientManager _clientManager;

    public FollowFriendEvent(IGameClientManager clientManager)
    {
        _clientManager = clientManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        var buddyId = packet.ReadInt();
        if (buddyId == 0 || buddyId == habbo.Id)
            return Task.CompletedTask;
        var client = _clientManager.GetClientByUserId(buddyId);
        var targetHabbo = client?.GetHabbo();
        if (targetHabbo == null)
            return Task.CompletedTask;
        if (!targetHabbo.InRoom)
        {
            session.Send(new FollowFriendFailedComposer(2));
            return Task.CompletedTask;
        }
        var targetRoom = targetHabbo.CurrentRoom;
        if (targetRoom == null)
            return Task.CompletedTask;
        if (habbo.CurrentRoom?.RoomId == targetRoom.RoomId)
            return Task.CompletedTask;
        session.Send(new RoomForwardComposer(targetRoom.RoomId));
        return Task.CompletedTask;
    }
}
