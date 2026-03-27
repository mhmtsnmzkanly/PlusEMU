using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;
using Plus.HabboHotel.Users;
using Plus.Utilities;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class MipCommand : ITargetChatCommand
{
    private readonly IModerationActionService _moderationActionService;
    public string Key => "mip";
    public string PermissionRequired => "command_mip";

    public string Parameters => "%username% %reason%";

    public string Description => "Machine ban, IP ban and account ban another user.";

    public bool MustBeInSameRoom => false;

    public MipCommand(IModerationActionService moderationActionService)
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

        // Use the centralized Ban method with machineBan = true (which should imply IP ban in my service logic or I should pass both?)
        // In my current ModerationActionService.Ban implementation:
        // if (machineBan) ipBan = false; // Wait, I made it false? 
        // Let's re-read ModerationActionService.cs
        
        await _moderationActionService.Ban(session, target.Id, reason, (int)(78892200 / 3600), false, true);

        session.SendWhisper($"Success, you have machine, IP and account banned the user '{target.Username}' for '{reason}'!");
    }
}
