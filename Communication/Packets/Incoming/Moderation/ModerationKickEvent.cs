using Plus.Core.Language;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class ModerationKickEvent : IPacketEvent
{
    private readonly IGameClientManager _clientManager;
    private readonly ILanguageManager _languageManager;

    public ModerationKickEvent(IGameClientManager clientManager, ILanguageManager languageManager)
    {
        _clientManager = clientManager;
        _languageManager = languageManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var sessionHabbo = session.GetHabbo();
        if (!(sessionHabbo?.Permissions?.HasRight("mod_kick") ?? false))
            return Task.CompletedTask;
        var userId = packet.ReadInt();
        packet.ReadString(); //message
        var client = _clientManager.GetClientByUserId(userId);
        var targetHabbo = client?.GetHabbo();
        if (targetHabbo == null || targetHabbo.CurrentRoom == null || targetHabbo.Id == sessionHabbo.Id)
            return Task.CompletedTask;
        if (targetHabbo.Rank >= sessionHabbo.Rank)
        {
            session.SendNotification(_languageManager.TryGetValue("moderation.kick.disallowed"));
            return Task.CompletedTask;
        }
        if (client == null)
            return Task.CompletedTask;
        sessionHabbo.CurrentRoom?.GetRoomUserManager().RemoveUserFromRoom(client, true);
        return Task.CompletedTask;
    }
}
