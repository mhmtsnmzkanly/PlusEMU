using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Ambassadors;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class AmbassadorAlertEvent : IPacketEvent
{
    private readonly IAmbassadorsManager _ambassadorsManager;
    private readonly IGameClientManager _clientManager;
    private readonly IUserDataFactory _userDataFactory;

    public AmbassadorAlertEvent(IAmbassadorsManager ambassadorsManager, IGameClientManager clientManager, IUserDataFactory userDataFactory)
    {
        _ambassadorsManager = ambassadorsManager;
        _clientManager = clientManager;
        _userDataFactory = userDataFactory;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var userid = packet.ReadInt();
        var target = _clientManager.GetClientByUserId(userid)?.GetHabbo() ?? 
                     await _userDataFactory.GetUserDataByIdAsync(userid);
        if (target == null)
            return;

        await _ambassadorsManager.Warn(session.GetHabbo(), target, "Alert");
    }
}
