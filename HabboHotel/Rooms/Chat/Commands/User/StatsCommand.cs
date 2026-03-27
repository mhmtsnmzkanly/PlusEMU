using System.Text;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class StatsCommand : IChatCommand
{
    public string Key => "stats";
    public string PermissionRequired => "command_stats";

    public string Parameters => "";

    public string Description => "View your current statistics.";

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo?.HabboStats == null)
            return;

        var minutes = habbo.HabboStats.OnlineTime / 60;
        var hours = minutes / 60;
        var onlineTime = Convert.ToInt32(hours);
        var s = onlineTime == 1 ? "" : "s";
        var habboInfo = new StringBuilder();
        habboInfo.Append("Your account stats:\r\r");
        habboInfo.Append("Currency Info:\r");
        habboInfo.Append($"Credits: {habbo.Credits}\r");
        habboInfo.Append($"Duckets: {habbo.Duckets}\r");
        habboInfo.Append($"Diamonds: {habbo.Diamonds}\r");
        habboInfo.Append($"Online Time: {onlineTime} Hour{s}\r");
        habboInfo.Append($"Respects: {habbo.HabboStats.Respect}\r");
        habboInfo.Append($"GOTW Points: {habbo.GotwPoints}\r\r");
        session.SendNotification(habboInfo.ToString());
    }
}
