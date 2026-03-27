using System.Collections.Concurrent;
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
        _members = new();
        _requests = new();
        _administrators = new();
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

    public void InitMembers(IDbConnection connection)
    {
        _administrators.Clear();
        _members.Clear();
        _requests.Clear();

        var memberships = connection.Query("SELECT `user_id`, `rank` FROM `group_memberships` WHERE `group_id` = @id", new { id = Id });
        foreach (var membership in memberships)
        {
            var userId = Convert.ToInt32(membership.user_id);
            var isAdmin = Convert.ToInt32(membership.rank) != 0;
            if (isAdmin)
                _administrators.Add(userId);
            else
                _members.Add(userId);
        }

        var requests = connection.Query("SELECT `user_id` FROM `group_requests` WHERE `group_id` = @id", new { id = Id });
        foreach (var request in requests)
        {
            var userId = Convert.ToInt32(request.user_id);
            if (!_members.Contains(userId) && !_administrators.Contains(userId)) _requests.Add(userId);
        }
    }

    public bool IsMember(int id) => _members.Contains(id) || _administrators.Contains(id);

    public bool IsAdmin(int id) => _administrators.Contains(id);

    public bool HasRequest(int id) => _requests.Contains(id);

    public void MakeAdmin(int id)
    {
        if (_members.Contains(id))
            _members.Remove(id);
        if (!_administrators.Contains(id))
            _administrators.Add(id);
    }

    public void TakeAdmin(int userId)
    {
        if (!_administrators.Contains(userId))
            return;
        _administrators.Remove(userId);
        _members.Add(userId);
    }

    public void AddMember(int id)
    {
        if (IsMember(id) || Type == GroupType.Locked && _requests.Contains(id))
            return;
        if (Type == GroupType.Locked)
            _requests.Add(id);
        else
            _members.Add(id);
    }

    public void DeleteMember(int id)
    {
        if (_members.Contains(id))
            _members.Remove(id);
        else if (_administrators.Contains(id))
            _administrators.Add(id);
    }

    public void HandleRequest(int id, bool accepted)
    {
        if (accepted)
            _members.Add(id);
        if (_requests.Contains(id))
            _requests.Remove(id);
    }

    public RoomData? GetRoom(IRoomFactory roomFactory)
    {
        if (_room == null)
        {
            if (!roomFactory.TryGetData(RoomId, out var data))
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
