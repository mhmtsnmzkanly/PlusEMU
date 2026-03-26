using Plus.Database;
using Plus.HabboHotel.GameClients;
using Dapper;
using Plus.HabboHotel.Moderation;
using Plus.HabboHotel.Users;
using Plus.Utilities;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class IpBanCommand : ITargetChatCommand
{
    private readonly IDatabase _database;
    private readonly IModerationManager _moderationManager;
    public string Key => "ipban";
    public string PermissionRequired => "command_ip_ban";

    public string Parameters => "%username%";

    public string Description => "IP and account ban another user.";

    public bool MustBeInSameRoom => true;

    public IpBanCommand(IDatabase database, IModerationManager moderationManager)
    {
        _database = database;
        _moderationManager = moderationManager;
    }

    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        var habbo = session.GetHabbo();
        var targetClient = target.Client;
        var moderatorName = habbo?.Username ?? string.Empty;
        var permissions = habbo?.Permissions;
        if ((target.Permissions?.HasRight("mod_tool") ?? false) && !(permissions?.HasRight("mod_ban_any") ?? false))
        {
            session.SendWhisper("Oops, you cannot ban that user.");
            return Task.CompletedTask;
        }
        var ipAddress = string.Empty;
        var expire = UnixTimestamp.GetNow() + 78892200;
        var username = target.Username;
        using var connection = _database.Connection();
        connection.Execute("UPDATE `user_info` SET `bans` = `bans` + '1' WHERE `user_id` = @id LIMIT 1", new { id = target.Id });
        ipAddress = connection.QuerySingleOrDefault<string>("SELECT `ip_last` FROM `users` WHERE `id` = @id LIMIT 1", new { id = target.Id });
        string reason;
        if (parameters.Any())
            reason = CommandManager.MergeParams(parameters);
        else
            reason = "No reason specified.";
        if (!string.IsNullOrEmpty(ipAddress))
            _moderationManager.BanUser(moderatorName, ModerationBanType.Ip, ipAddress, reason, expire);
        _moderationManager.BanUser(moderatorName, ModerationBanType.Username, target.Username, reason, expire);
        targetClient?.Disconnect();
        session.SendWhisper($"Success, you have IP and account banned the user '{username}' for '{reason}'!");
        return Task.CompletedTask;
    }
}
