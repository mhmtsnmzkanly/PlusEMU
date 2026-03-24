using Plus.Database;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Users;

internal class SetUserFocusPreferenceEvent : IPacketEvent
{
    private readonly IDatabase _database;

    public SetUserFocusPreferenceEvent(IDatabase database)
    {
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        var focusPreference = packet.ReadBool();
        habbo.FocusPreference = focusPreference;
        using var dbClient = _database.GetQueryReactor();
        dbClient.SetQuery("UPDATE `users` SET `focus_preference` = @focusPreference WHERE `id` = @habboId LIMIT 1");
        dbClient.AddParameter("habboId", habbo.Id);
        dbClient.AddParameter("focusPreference", focusPreference);
        dbClient.RunQuery();
        return Task.CompletedTask;
    }
}
