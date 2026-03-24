using Plus.Database;
using Plus.HabboHotel.GameClients;
using Dapper;

namespace Plus.Communication.Packets.Incoming.Users;

internal class SetMessengerInviteStatusEvent : IPacketEvent
{
    private readonly IDatabase _database;

    public SetMessengerInviteStatusEvent(IDatabase database)
    {
        _database = database;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        var allowMessengerInvites = packet.ReadBool();
        habbo.AllowMessengerInvites = allowMessengerInvites;
        using var connection = _database.Connection();
        await connection.ExecuteAsync("UPDATE users SET ignore_invites = @ignoreInvites WHERE id = @userId LIMIT 1",
            new { ignoreInvites = allowMessengerInvites, userId = habbo.Id });
    }
}
