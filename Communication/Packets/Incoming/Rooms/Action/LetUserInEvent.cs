using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class LetUserInEvent : RoomPacketEvent
{
    private readonly IGameClientManager _clientManager;

    public LetUserInEvent(IGameClientManager clientManager)
    {
        _clientManager = clientManager;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        if (!room.CheckRights(session))
            return Task.CompletedTask;
        var name = packet.ReadString();
        var accepted = packet.ReadBool();
        var client = _clientManager.GetClientByUsername(name);
        var habbo = client?.GetHabbo();
        if (habbo == null || client == null)
            return Task.CompletedTask;
        if (accepted)
        {
            habbo.RoomAuthOk = true;
            client.Send(new FlatAccessibleComposer(""));
            room.SendPacket(new FlatAccessibleComposer(habbo.Username), true);
        }
        else
        {
            client.Send(new FlatAccessDeniedComposer(""));
            room.SendPacket(new FlatAccessDeniedComposer(habbo.Username), true);
        }
        return Task.CompletedTask;
    }
}
