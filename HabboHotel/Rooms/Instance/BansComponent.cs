using System.Collections.Concurrent;
using Dapper;
using Plus.Utilities;

namespace Plus.HabboHotel.Rooms.Instance;

public class BansComponent
{
    private sealed class RoomBanRow
    {
        public int UserId { get; init; }

        public double Expire { get; init; }
    }

    /// <summary>
    /// The bans collection for storing them for this room.
    /// </summary>
    private ConcurrentDictionary<int, double> _bans;
    /// <summary>
    /// The RoomInstance that created this BanComponent.
    /// </summary>
    private Room? _instance;

    /// <summary>
    /// Create the BanComponent for the RoomInstance.
    /// </summary>
    /// <param name="instance">The instance that created this component.</param>
    public BansComponent(Room instance)
    {
        _instance = instance;
        _bans = new();
        using var connection = _instance.GetDatabase().Connection();
        var getBans = connection.Query<RoomBanRow>(
            "SELECT `user_id` AS UserId, `expire` AS Expire FROM `room_bans` WHERE `room_id` = @roomId AND `expire` > UNIX_TIMESTAMP();",
            new { roomId = _instance.Id });
        foreach (var row in getBans)
            _bans.TryAdd(row.UserId, row.Expire);
    }

    public int Count => _bans.Count;

    public void Ban(RoomUser avatar, double time)
    {
        if (_instance == null || avatar == null || _instance.CheckRights(avatar.GetClient(), true) || IsBanned(avatar.UserId))
            return;
        var banTime = UnixTimestamp.GetNow() + time;
        if (!_bans.TryAdd(avatar.UserId, banTime))
            _bans[avatar.UserId] = banTime;
        using (var connection = _instance.GetDatabase().Connection())
        {
            connection.Execute("REPLACE INTO `room_bans` (`user_id`,`room_id`,`expire`) VALUES (@uid, @rid, @expire);",
                new { rid = _instance.Id, uid = avatar.UserId, expire = banTime });
        }
        _instance.GetRoomUserManager().RemoveUserFromRoom(avatar.GetClient(), true, true);
    }

    public bool IsBanned(int userId)
    {
        if (_instance == null || !_bans.ContainsKey(userId))
            return false;
        var banTime = _bans[userId] - UnixTimestamp.GetNow();
        if (banTime <= 0)
        {
            _bans.TryRemove(userId, out var time);
            using var connection = _instance.GetDatabase().Connection();
            connection.Execute("DELETE FROM `room_bans` WHERE `room_id` = @rid AND `user_id` = @uid;",
                new { rid = _instance.Id, uid = userId });
            return false;
        }
        return true;
    }

    public bool Unban(int userId)
    {
        if (_instance == null || !_bans.ContainsKey(userId))
            return false;
        if (_bans.TryRemove(userId, out var time))
        {
            using var connection = _instance.GetDatabase().Connection();
            connection.Execute("DELETE FROM `room_bans` WHERE `room_id` = @rid AND `user_id` = @uid;",
                new { rid = _instance.Id, uid = userId });
            return true;
        }
        return false;
    }

    public List<int> BannedUsers()
    {
        if (_instance == null)
            return new List<int>();

        using var connection = _instance.GetDatabase().Connection();
        return connection.Query<int>(
            "SELECT `user_id` FROM `room_bans` WHERE `room_id` = @roomId AND `expire` > UNIX_TIMESTAMP();",
            new { roomId = _instance.Id })
            .Distinct()
            .ToList();
    }

    public void Cleanup()
    {
        _bans.Clear();
        _instance = null;
    }
}
