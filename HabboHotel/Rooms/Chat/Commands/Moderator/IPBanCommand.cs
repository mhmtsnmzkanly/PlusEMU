using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;
using Plus.HabboHotel.Users;
using Plus.Utilities;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class IpBanCommand : ITargetChatCommand
{
    private readonly IModerationActionService _moderationActionService;
    public string Key => "ipban";
    public string PermissionRequired => "command_ip_ban";

    public string Parameters => "%username% %reason%";

    public string Description => "IP and account ban another user.";

    public bool MustBeInSameRoom => true;

    public IpBanCommand(IModerationActionService moderationActionService)
    {
        _moderationActionService = moderationActionService;
    }

    public async Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        var habbo = session.GetHabbo();
        var moderatorName = habbo?.Username ?? "System";
        var permissions = habbo?.Permissions;
        if ((target.Permissions?.HasRight("mod_tool") ?? false) && !(permissions?.HasRight("mod_ban_any") ?? false))
        {
            session.SendWhisper("Oops, you cannot ban that user.");
            return;
        }

        var expire = UnixTimestamp.GetNow() + 78892200; // Permanent (approx 2.5 years)
        string reason;
        if (parameters.Any())
            reason = CommandManager.MergeParams(parameters);
        else
            reason = "No reason specified.";

        // Use the centralized Ban method with ipBan = true
        await _moderationActionService.Ban(session, target.Id, reason, (int)(78892200 / 3600), true, false);

        session.SendWhisper($"Success, you have IP and account banned the user '{target.Username}' for '{reason}'!");
    }
}
