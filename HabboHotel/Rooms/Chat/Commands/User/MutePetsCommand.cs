using Plus.Database;
using Plus.HabboHotel.GameClients;
using Dapper;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class MutePetsCommand : IChatCommand
{
    private readonly IDatabase _database;
    public string Key => "mutepets";
    public string PermissionRequired => "command_mute_pets";

    public string Parameters => "";

    public string Description => "Ignore bot chat or enable it again.";

    public MutePetsCommand(IDatabase database)
    {
        _database = database;
    }

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        habbo.AllowPetSpeech = !habbo.AllowPetSpeech;
        using var connection = _database.Connection();
        connection.Execute("UPDATE `users` SET `pets_muted` = @muted WHERE `id` = @id LIMIT 1", new { muted = habbo.AllowPetSpeech, id = habbo.Id });
        if (habbo.AllowPetSpeech)
            session.SendWhisper("Change successful, you can no longer see speech from pets.");
        else
            session.SendWhisper("Change successful, you can now see speech from pets.");
    }
}
