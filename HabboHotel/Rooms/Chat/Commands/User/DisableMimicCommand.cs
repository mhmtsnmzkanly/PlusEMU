using Plus.Database;
using Dapper;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class DisableMimicCommand : IChatCommand
{
    private readonly IDatabase _database;
    public string Key => "disablemimic";
    public string PermissionRequired => "command_disable_mimic";

    public string Parameters => "";

    public string Description => "Allows you to disable the ability to be mimiced or to enable the ability to be mimiced.";

    public DisableMimicCommand(IDatabase database)
    {
        _database = database;
    }

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        habbo.AllowMimic = !habbo.AllowMimic;
        session.SendWhisper($"You're {(habbo.AllowMimic ? "now" : "no longer")} able to be mimiced.");
        using var connection = _database.Connection();
        connection.Execute("UPDATE users SET allow_mimic = @AllowMimic WHERE id = @userId LIMIT 1",
            new { AllowMimic = habbo.AllowMimic, userId = habbo.Id });
    }
}
