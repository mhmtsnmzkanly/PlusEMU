using Plus.Database;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class MuteBotsCommand : IChatCommand
{
    private readonly IDatabase _database;
    public string Key => "mutebots";
    public string PermissionRequired => "command_mute_bots";

    public string Parameters => "";

    public string Description => "Ignore bot chat or enable it again.";

    public MuteBotsCommand(IDatabase database)
    {
        _database = database;
    }

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        habbo.AllowBotSpeech = !habbo.AllowBotSpeech;
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.RunQuery($"UPDATE `users` SET `bots_muted` = '{habbo.AllowBotSpeech}' WHERE `id` = '{habbo.Id}' LIMIT 1");
        }
        if (habbo.AllowBotSpeech)
            session.SendWhisper("Change successful, you can no longer see speech from bots.");
        else
            session.SendWhisper("Change successful, you can now see speech from bots.");
    }
}
