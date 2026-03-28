using Plus.Communication.Packets;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Ambassadors;
using Plus.HabboHotel.Users.UserData;

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
        var reporter = session.GetHabbo();
        if (reporter == null)
            return;

        var userid = packet.ReadInt();
        var target = _clientManager.GetClientByUserId(userid)?.GetHabbo() ?? 
                     await _userDataFactory.GetUserDataByIdAsync(userid);
        if (target == null)
            return;

        await _ambassadorsManager.Warn(reporter, target, "Alert");
    }
}
