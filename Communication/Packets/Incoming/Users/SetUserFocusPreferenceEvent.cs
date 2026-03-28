using Dapper;
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
        if (session.GetHabbo() is not { } habbo)
            return Task.CompletedTask;

        var focusPreference = packet.ReadBool();
        habbo.FocusPreference = focusPreference;
        using var db = _database.Connection();
        db.Execute("UPDATE `users` SET `focus_preference` = @fp WHERE `id` = @id LIMIT 1",
            new { fp = focusPreference, id = habbo.Id });
        return Task.CompletedTask;
    }
}
