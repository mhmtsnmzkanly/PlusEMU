using Plus.Database;
using Plus.HabboHotel.GameClients;
using Dapper;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class DisableForcedFxCommand : IChatCommand
{
    private readonly IDatabase _database;
    public string Key => "forced_effects";
    public string PermissionRequired => "command_forced_effects";

    public string Parameters => "";

    public string Description => "Gives you the ability to ignore or allow forced effects.";

    public DisableForcedFxCommand(IDatabase database)
    {
        _database = database;
    }

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        habbo.DisableForcedEffects = !habbo.DisableForcedEffects;
        using (var connection = _database.Connection())
        {
            connection.Execute("UPDATE users SET disable_forced_effects = @DisableForcedEffects WHERE id = @userId LIMIT 1",
                new { DisableForcedEffects = habbo.DisableForcedEffects, userId = habbo.Id });
        }
        session.SendWhisper($"Forced FX mode is now {(habbo.DisableForcedEffects ? "disabled!" : "enabled!")}");
    }
}
