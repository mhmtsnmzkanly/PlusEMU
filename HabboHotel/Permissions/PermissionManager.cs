using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Database;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Permissions;

public sealed class PermissionManager : IPermissionManager
{
    private readonly IDatabase _database;
    private readonly ILogger<PermissionManager> _logger;

    private readonly Dictionary<string, PermissionCommand> _commands = new();
    private readonly Dictionary<int, List<string>> _permissionGroupRights = new();
    private readonly Dictionary<int, PermissionGroup> _permissionGroups = new();
    private readonly Dictionary<int, Permission> _permissions = new();
    private readonly Dictionary<int, List<string>> _permissionSubscriptionRights = new();

    public PermissionManager(IDatabase database, ILogger<PermissionManager> logger)
    {
        _database = database;
        _logger = logger;
    }

    public void Init()
    {
        _permissions.Clear();
        _commands.Clear();
        _permissionGroups.Clear();
        _permissionGroupRights.Clear();

        using var db = _database.Connection();

        // Load permissions
        foreach (var row in db.Query("SELECT `id`, `permission`, `description` FROM `permissions`"))
            _permissions.Add((int)row.id, new((int)row.id, ((string?)row.permission) ?? string.Empty, ((string?)row.description) ?? string.Empty));

        // Load commands
        foreach (var row in db.Query("SELECT `command`, `group_id`, `subscription_id` FROM `permissions_commands`"))
        {
            var command = (string?)row.command;
            if (string.IsNullOrEmpty(command)) continue;
            _commands.Add(command, new(command, (int)row.group_id, (int)row.subscription_id));
        }

        // Load groups
        foreach (var row in db.Query("SELECT `id`, `name`, `description`, `badge_code` FROM `permissions_groups`"))
            _permissionGroups.Add((int)row.id, new(((string?)row.name) ?? string.Empty, ((string?)row.description) ?? string.Empty, ((string?)row.badge_code) ?? string.Empty));

        // Load group rights
        foreach (var row in db.Query("SELECT `group_id`, `permission_id` FROM `permissions_rights`"))
        {
            var groupId = (int)row.group_id;
            var permissionId = (int)row.permission_id;
            if (!_permissionGroups.ContainsKey(groupId)) continue;
            if (!_permissions.TryGetValue(permissionId, out var permission)) continue;
            if (_permissionGroupRights.ContainsKey(groupId))
                _permissionGroupRights[groupId].Add(permission.PermissionName);
            else
                _permissionGroupRights.Add(groupId, [permission.PermissionName]);
        }

        // Load subscription rights
        foreach (var row in db.Query("SELECT `permission_id`, `subscription_id` FROM `permissions_subscriptions`"))
        {
            var permissionId = (int)row.permission_id;
            var subscriptionId = (int)row.subscription_id;
            if (!_permissions.TryGetValue(permissionId, out var permission)) continue;
            if (_permissionSubscriptionRights.ContainsKey(subscriptionId))
                _permissionSubscriptionRights[subscriptionId].Add(permission.PermissionName);
            else
                _permissionSubscriptionRights.Add(subscriptionId, [permission.PermissionName]);
        }

        _logger.LogInformation("Loaded {PermCount} permissions.", _permissions.Count);
        _logger.LogInformation("Loaded {GroupCount} permissions groups.", _permissionGroups.Count);
        _logger.LogInformation("Loaded {RightCount} permissions group rights.", _permissionGroupRights.Count);
        _logger.LogInformation("Loaded {SubRightCount} permissions subscription rights.", _permissionSubscriptionRights.Count);
    }

    public bool TryGetGroup(int id, out PermissionGroup? group) => _permissionGroups.TryGetValue(id, out group);

    public List<string> GetPermissionsForPlayer(Habbo player)
    {
        var permissionSet = new List<string>();
        if (_permissionGroupRights.TryGetValue(player.Rank, out var permRights)) permissionSet.AddRange(permRights);
        if (_permissionSubscriptionRights.TryGetValue(player.VipRank, out var subscriptionRights)) permissionSet.AddRange(subscriptionRights);
        return permissionSet;
    }

    public List<string> GetCommandsForPlayer(Habbo player)
    {
        return _commands.Where(x => player.Rank >= x.Value.GroupId && player.VipRank >= x.Value.SubscriptionId).Select(x => x.Key).ToList();
    }
}
