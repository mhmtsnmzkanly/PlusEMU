using Plus.Database;
using Plus.HabboHotel.GameClients;
using Dapper;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class DisableGiftsCommand : IChatCommand
{
    private readonly IDatabase _database;
    public string Key => "disablegifts";
    public string PermissionRequired => "command_disable_gifts";

    public string Parameters => "";

    public string Description => "Allows you to disable the ability to receive gifts or to enable the ability to receive gifts.";

    public DisableGiftsCommand(IDatabase database)
    {
        _database = database;
    }

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        habbo.AllowGifts = !habbo.AllowGifts;
        session.SendWhisper($"You're {(habbo.AllowGifts ? "now" : "no longer")} accepting gifts.");
        using var connection = _database.Connection();
        connection.Execute("UPDATE `users` SET `allow_gifts` = @AllowGifts WHERE `id` = @id LIMIT 1", new { AllowGifts = habbo.AllowGifts, id = habbo.Id });
    }
}
