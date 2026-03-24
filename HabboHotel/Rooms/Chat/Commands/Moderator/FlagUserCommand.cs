using Plus.Communication.Packets.Outgoing.Handshake;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class FlagUserCommand : ITargetChatCommand
{
    public string Key => "flaguser";
    public string PermissionRequired => "command_flaguser";

    public string Parameters => "%username%";

    public string Description => "Forces the specified user to change their name.";

    public bool MustBeInSameRoom => false;

    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        var targetClient = target.Client;
        if ((target.Permissions?.HasRight("mod_tool") ?? false))
        {
            session.SendWhisper("You are not allowed to flag that user.");
            return Task.CompletedTask;
        }
        if (targetClient == null)
            return Task.CompletedTask;
        target.LastNameChange = 0;
        target.ChangingName = true;
        targetClient.SendNotification("Please be aware that if your username is deemed as inappropriate, you will be banned without question.\r\rAlso note that Staff will NOT allow you to change your username again should you have an issue with what you have chosen.\r\rClose this window and click yourself to begin choosing a new username!");
        targetClient.Send(new UserObjectComposer(target));
        return Task.CompletedTask;
    }
}
