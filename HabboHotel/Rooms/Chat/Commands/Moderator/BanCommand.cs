using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;
using Plus.HabboHotel.Users;
using Plus.Utilities;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class BanCommand : ITargetChatCommand
{
    private readonly IModerationActionService _moderationActionService;
    public string Key => "ban";
    public string PermissionRequired => "command_ban";

    public string Parameters => "%username% %length% %reason% ";

    public string Description => "Remove a toxic player from the hotel for a fixed amount of time.";

    public bool MustBeInSameRoom => false;

    public BanCommand(IModerationActionService moderationActionService)
    {
        _moderationActionService = moderationActionService;
    }

    public async Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        var habbo = session.GetHabbo();
        var permissions = habbo?.Permissions;
        if ((target.Permissions?.HasRight("mod_soft_ban") ?? false) && !(permissions?.HasRight("mod_ban_any") ?? false))
        {
            session.SendWhisper("Oops, you cannot ban that user.");
            return;
        }

        double expire = 0;
        var length = parameters[0];
        if (string.IsNullOrEmpty(length) || length == "perm")
            expire = UnixTimestamp.GetNow() + 78892200;
        else
            expire = UnixTimestamp.GetNow() + Convert.ToDouble(length) * 3600;

        string reason;
        if (parameters.Length >= 2)
            reason = CommandManager.MergeParams(parameters, 1);
        else
            reason = "No reason specified.";

        await _moderationActionService.Ban(habbo?.Username ?? "System", ModerationBanType.Username, target.Username, reason, expire);

        if (target.TryGetClient(out var targetClient))
            targetClient.Disconnect();
        session.SendWhisper($"Success, you have account banned the user '{target.Username}' for {length} hour(s) with the reason '{reason}'!");
    }
}
