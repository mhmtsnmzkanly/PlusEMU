using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Chat.Styles;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Administrator;

internal class BubbleCommand : IChatCommand
{
    private readonly IChatStyleManager _chatStyleManager;
    public string Key => "bubble";
    public string PermissionRequired => "command_bubble";

    public string Parameters => "%id%";

    public string Description => "Use a custom bubble to chat with.";

    public BubbleCommand(IChatStyleManager chatStyleManager)
    {
        _chatStyleManager = chatStyleManager;
    }

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        var permissions = habbo?.Permissions;
        if (habbo == null)
            return;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null)
            return;
        if (parameters.Length == 0)
        {
            session.SendWhisper("Oops, you forgot to enter a bubble ID!");
            return;
        }
        if (!int.TryParse(parameters[0], out var bubble))
        {
            session.SendWhisper("Please enter a valid number.");
            return;
        }
        if (!_chatStyleManager.TryGetStyle(bubble, out var style) || style == null || style.RequiredRight.Length > 0 && !(permissions?.HasRight(style.RequiredRight) ?? false))
        {
            session.SendWhisper("Oops, you cannot use this bubble due to a rank requirement, sorry!");
            return;
        }
        user.LastBubble = bubble;
        habbo.CustomBubbleId = bubble;
        session.SendWhisper($"Bubble set to: {bubble}");
    }
}
