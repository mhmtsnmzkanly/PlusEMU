using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class GiveCommand : ITargetChatCommand
{
    public string Key => "give";
    public string PermissionRequired => "command_give";

    public string Parameters => "%username% %type% %amount%";

    public string Description => "";

    public bool MustBeInSameRoom => false;

    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Permissions == null)
            return Task.CompletedTask;

        if (parameters.Length < 3)
        {
            session.SendWhisper("Please enter a currency type and amount! (coins, duckets, diamonds, gotw)");
            return Task.CompletedTask;
        }

        var permissions = habbo.Permissions;
        var moderatorName = habbo.Username;
        var updateVal = parameters[1];
        switch (updateVal.ToLower())
        {
            case "coins":
            case "credits":
            {
                if (!(permissions?.HasCommand("command_give_coins") ?? false))
                {
                    session.SendWhisper("Oops, it appears that you do not have the permissions to use this command!");
                    break;
                }
                if (int.TryParse(parameters[2], out var amount))
                {
                    target.Credits = target.Credits += amount;
                    target.Client.Send(new CreditBalanceComposer(target.Credits));
                    if (target.Id != habbo.Id)
                        target.Client.SendNotification($"{moderatorName} has given you {amount} Credit(s)!");
                    session.SendWhisper($"Successfully given {amount} Credit(s) to {target.Username}!");
                    break;
                }
                session.SendWhisper("Oops, that appears to be an invalid amount!");
                break;
            }
            case "pixels":
            case "duckets":
            {
                if (!(permissions?.HasCommand("command_give_pixels") ?? false))
                {
                    session.SendWhisper("Oops, it appears that you do not have the permissions to use this command!");
                    break;
                }
                if (int.TryParse(parameters[2], out var amount))
                {
                    target.Duckets += amount;
                    target.Client.Send(new HabboActivityPointNotificationComposer(target.Duckets, amount));
                    if (target.Id != habbo.Id)
                        target.Client.SendNotification($"{moderatorName} has given you {amount} Ducket(s)!");
                    session.SendWhisper($"Successfully given {amount} Ducket(s) to {target.Username}!");
                    break;
                }
                session.SendWhisper("Oops, that appears to be an invalid amount!");
                break;
            }
            case "diamonds":
            {
                if (!(permissions?.HasCommand("command_give_diamonds") ?? false))
                {
                    session.SendWhisper("Oops, it appears that you do not have the permissions to use this command!");
                    break;
                }
                if (int.TryParse(parameters[2], out var amount))
                {
                    target.Diamonds += amount;
                    target.Client.Send(new HabboActivityPointNotificationComposer(target.Diamonds, amount, 5));
                    if (target.Id != habbo.Id)
                        target.Client.SendNotification($"{moderatorName} has given you {amount} Diamond(s)!");
                    session.SendWhisper($"Successfully given {amount} Diamond(s) to {target.Username}!");
                    break;
                }
                session.SendWhisper("Oops, that appears to be an invalid amount!");
                break;
            }
            case "gotw":
            case "gotwpoints":
            {
                if (!(permissions?.HasCommand("command_give_gotw") ?? false))
                {
                    session.SendWhisper("Oops, it appears that you do not have the permissions to use this command!");
                    break;
                }
                if (int.TryParse(parameters[2], out var amount))
                {
                    target.GotwPoints = target.GotwPoints + amount;
                    target.Client.Send(new HabboActivityPointNotificationComposer(target.GotwPoints, amount, 103));
                    if (target.Id != habbo.Id)
                        target.Client.SendNotification($"{moderatorName} has given you {amount} GOTW Point(s)!");
                    session.SendWhisper($"Successfully given {amount} GOTW point(s) to {target.Username}!");
                    break;
                }
                session.SendWhisper("Oops, that appears to be an invalid amount!");
                break;
            }
            default:
                session.SendWhisper($"'{updateVal}' is not a valid currency!");
                break;
        }
        return Task.CompletedTask;
    }
}
