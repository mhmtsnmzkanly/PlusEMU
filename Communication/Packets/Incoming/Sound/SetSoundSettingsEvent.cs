using Plus.Database;
using Plus.HabboHotel.GameClients;
using Dapper;

namespace Plus.Communication.Packets.Incoming.Sound;

internal class SetSoundSettingsEvent : IPacketEvent
{
    private readonly IDatabase _database;

    public SetSoundSettingsEvent(IDatabase database)
    {
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        var volume = "";
        for (var i = 0; i < 3; i++)
        {
            var vol = packet.ReadInt();
            if (vol < 0 || vol > 100) vol = 100;
            if (i < 2)
                volume += $"{vol},";
            else
                volume += vol;
        }
        using var connection = _database.Connection();
        connection.Execute("UPDATE users SET volume = @volume WHERE id = @id LIMIT 1", new { volume = volume, id = habbo.Id });
        return Task.CompletedTask;
    }
}
