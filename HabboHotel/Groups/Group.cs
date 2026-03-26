using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Dapper;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Groups;

public class Group
{
    private readonly List<int> _administrators;
    private readonly List<int> _members;
    private readonly List<int> _requests;

    private RoomData? _room;
    public bool HasForum;

    public Group(int id, string name, string description, string badge, uint roomId, int owner, int time, int type, int colour1, int colour2, int adminOnlyDeco, bool hasForum)
    {
        Id = id;
        Name = name;
        Description = description;
        RoomId = roomId;
        Badge = badge;
        CreateTime = time;
        CreatorId = owner;
        Colour1 = colour1 == 0 ? 1 : colour1;
        Colour2 = colour2 == 0 ? 1 : colour2;
        HasForum = hasForum;
        Type = (GroupType)type;
        AdminOnlyDeco = adminOnlyDeco;
        ForumEnabled = ForumEnabled;
        _members = new();
        _requests = new();
        _administrators = new();
        InitMembers();
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public int AdminOnlyDeco { get; set; }
    public string Badge { get; set; }
    public int CreateTime { get; set; }
    public int CreatorId { get; set; }
    public string Description { get; set; }
    public uint RoomId { get; set; }
    public int Colour1 { get; set; }
    public int Colour2 { get; set; }
    public bool ForumEnabled { get; set; }
    public GroupType Type { get; set; }

    public List<int> GetMembers => _members.ToList();

    public List<int> GetRequests => _requests.ToList();

    public List<int> GetAdministrators => _administrators.ToList();

    public List<int> GetAllMembers
    {
        get
        {
            var members = new List<int>(_administrators.ToList());
            members.AddRange(_members.ToList());
            return members;
        }
    }

    public int MemberCount => _members.Count + _administrators.Count;

    public int RequestCount => _requests.Count;

    public void InitMembers()
    {
        using var connection = PlusEnvironment.DatabaseManager.Connection();
        var memberships = connection.Query("SELECT `user_id`, `rank` FROM `group_memberships` WHERE `group_id` = @id", new { id = Id });
        foreach (var membership in memberships)
        {
            var userId = Convert.ToInt32(membership.user_id);
            var isAdmin = Convert.ToInt32(membership.rank) != 0;
            if (isAdmin)
            {
                if (!_administrators.Contains(userId))
                    _administrators.Add(userId);
            }
            else
            {
                if (!_members.Contains(userId))
                    _members.Add(userId);
            }
        }
        
        var requests = connection.Query("SELECT `user_id` FROM `group_requests` WHERE `group_id` = @id", new { id = Id });
        foreach (var request in requests)
        {
            var userId = Convert.ToInt32(request.user_id);
            if (_members.Contains(userId) || _administrators.Contains(userId))
                connection.Execute("DELETE FROM `group_requests` WHERE `group_id` = @gid AND `user_id` = @uid", new { gid = Id, uid = userId });
            else if (!_requests.Contains(userId)) _requests.Add(userId);
        }
    }

    public bool IsMember(int id) => _members.Contains(id) || _administrators.Contains(id);

    public bool IsAdmin(int id) => _administrators.Contains(id);

    public bool HasRequest(int id) => _requests.Contains(id);

    public void MakeAdmin(int id)
    {
        if (_members.Contains(id))
            _members.Remove(id);
        using var connection = PlusEnvironment.DatabaseManager.Connection();
        connection.Execute("UPDATE group_memberships SET `rank` = '1' WHERE `user_id` = @uid AND `group_id` = @gid LIMIT 1", new { gid = Id, uid = id });
        if (!_administrators.Contains(id))
            _administrators.Add(id);
    }

    public void TakeAdmin(int userId)
    {
        if (!_administrators.Contains(userId))
            return;
        using var connection = PlusEnvironment.DatabaseManager.Connection();
        connection.Execute("UPDATE group_memberships SET `rank` = '0' WHERE user_id = @uid AND group_id = @gid", new { gid = Id, uid = userId });
        _administrators.Remove(userId);
        _members.Add(userId);
    }

    public void AddMember(int id)
    {
        if (IsMember(id) || Type == GroupType.Locked && _requests.Contains(id))
            return;
        using var connection = PlusEnvironment.DatabaseManager.Connection();
        if (IsAdmin(id))
        {
            connection.Execute("UPDATE `group_memberships` SET `rank` = '0' WHERE user_id = @uid AND group_id = @gid", new { gid = Id, uid = id });
            _administrators.Remove(id);
            _members.Add(id);
        }
        else if (Type == GroupType.Locked)
        {
            connection.Execute("INSERT INTO `group_requests` (user_id, group_id) VALUES (@uid, @gid)", new { gid = Id, uid = id });
            _requests.Add(id);
        }
        else
        {
            connection.Execute("INSERT INTO `group_memberships` (user_id, group_id) VALUES (@uid, @gid)", new { gid = Id, uid = id });
            _members.Add(id);
        }
    }

    public void DeleteMember(int id)
    {
        if (IsMember(id))
        {
            if (_members.Contains(id))
                _members.Remove(id);
        }
        else if (IsAdmin(id))
        {
            if (_administrators.Contains(id))
                _administrators.Remove(id);
        }
        else
            return;
        using var connection = PlusEnvironment.DatabaseManager.Connection();
        connection.Execute("DELETE FROM group_memberships WHERE user_id=@uid AND group_id=@gid LIMIT 1", new { gid = Id, uid = id });
    }

    public void HandleRequest(int id, bool accepted)
    {
        using var connection = PlusEnvironment.DatabaseManager.Connection();
        if (accepted)
        {
            connection.Execute("INSERT INTO group_memberships (user_id, group_id) VALUES (@uid, @gid)", new { gid = Id, uid = id });
            _members.Add(id);
        }
        connection.Execute("DELETE FROM group_requests WHERE user_id=@uid AND group_id=@gid LIMIT 1", new { gid = Id, uid = id });
        
        if (_requests.Contains(id))
            _requests.Remove(id);
    }

    public RoomData? GetRoom()
    {
        if (_room == null)
        {
            if (!RoomFactory.TryGetData(RoomId, out var data))
                return null;
            _room = data;
            return data;
        }
        return _room;
    }


    public void ClearRequests()
    {
        _requests.Clear();
    }

    public void Dispose()
    {
        _requests.Clear();
        _members.Clear();
        _administrators.Clear();
    }
}
