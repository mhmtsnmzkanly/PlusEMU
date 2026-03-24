using Plus.Database;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class ModerationCautionEvent : IPacketEvent
{
    private readonly IGameClientManager _clientManager;
    private readonly IDatabase _database;

    public ModerationCautionEvent(IGameClientManager clientManager, IDatabase database)
    {
        _clientManager = clientManager;
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var moderator = session.GetHabbo();
        if (moderator?.Permissions == null || !moderator.Permissions.HasRight("mod_caution"))
            return Task.CompletedTask;
        var userId = packet.ReadInt();
        var message = packet.ReadString();
        var client = _clientManager.GetClientByUserId(userId);
        var targetHabbo = client?.GetHabbo();
        if (targetHabbo == null)
            return Task.CompletedTask;
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.RunQuery($"UPDATE `user_info` SET `cautions` = `cautions` + '1' WHERE `user_id` = '{targetHabbo.Id}' LIMIT 1");
        }
        client.SendNotification(message);
        return Task.CompletedTask;
    }
}
