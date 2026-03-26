using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Core.FigureData;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Dapper;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class FacelessCommand : IChatCommand
{
    private readonly IFigureDataManager _figureDataManager;
    private readonly IDatabase _database;
    public string Key => "faceless";
    public string PermissionRequired => "command_faceless";

    public string Parameters => "";

    public string Description => "Allows you to go faceless!";

    public FacelessCommand(IFigureDataManager figureDataManager, IDatabase database)
    {
        _figureDataManager = figureDataManager;
        _database = database;
    }

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Clothing == null)
            return;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null || user.GetClient() == null)
            return;
        string[] headParts;
        var figureParts = habbo.Look.Split('.');
        foreach (var part in figureParts)
        {
            if (part.StartsWith("hd"))
            {
                headParts = part.Split('-');
                if (!headParts[1].Equals("99999"))
                    headParts[1] = "99999";
                else
                    return;
                habbo.Look = habbo.Look.Replace(part, $"hd-{headParts[1]}-{headParts[2]}");
                break;
            }
        }
        habbo.Look = _figureDataManager.ProcessFigure(habbo.Look, habbo.Gender, habbo.Clothing.GetClothingParts, true);
        using var connection = _database.Connection();
        connection.Execute("UPDATE `users` SET `look` = @look WHERE `id` = @id LIMIT 1", new { look = habbo.Look, id = habbo.Id });
        session.Send(new UserChangeComposer(user, true));
        habbo.CurrentRoom?.SendPacket(new UserChangeComposer(user, false));
    }
}
