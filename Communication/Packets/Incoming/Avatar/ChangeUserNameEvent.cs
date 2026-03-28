using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.Communication.Packets.Outgoing.Users;
using Plus.Database;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users;
using Plus.Utilities;
using Dapper;
using Plus.HabboHotel.Users.UserData;

namespace Plus.Communication.Packets.Incoming.Avatar;

internal class ChangeUserNameEvent : IPacketEvent
{
    private readonly IUserDataFactory _userDataFactory;
    private readonly IGameClientManager _clientManager;
    private readonly IRoomManager _roomManager;
    private readonly IRoomService _roomService;
    private readonly IAchievementService _achievementService;
    private readonly IDatabase _database;

    public ChangeUserNameEvent(IUserDataFactory userDataFactory, IGameClientManager clientManager, IRoomManager roomManager, IRoomService roomService, IAchievementService achievementService, IDatabase database)
    {
        _userDataFactory = userDataFactory;
        _clientManager = clientManager;
        _roomManager = roomManager;
        _roomService = roomService;
        _achievementService = achievementService;
        _database = database;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        var room = habbo.CurrentRoom;
        if (room == null)
            return;
        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Username);
        if (user == null)
            return;
        var newName = packet.ReadString();
        var oldName = habbo.Username;
        if (newName == oldName)
        {
            habbo.ChangeName(_database, oldName);
            session.Send(new UpdateUsernameComposer(newName));
            return;
        }
        if (!CanChangeName(habbo))
        {
            session.SendNotification("Oops, it appears you currently cannot change your username!");
            return;
        }
        var inUse = await _userDataFactory.HabboExists(newName);
        if (inUse)
            return;
        var letters = newName.ToLower().ToCharArray();
        const string allowedCharacters = "abcdefghijklmnopqrstuvwxyz.,_-;:?!1234567890";
        if (letters.Any(chr => !allowedCharacters.Contains(chr)))
            return;
        if (!(habbo.Permissions?.HasRight("mod_tool") ?? false) && newName.ToLower().Contains("mod") || newName.ToLower().Contains("adm") || newName.ToLower().Contains("admin")
            || newName.ToLower().Contains("m0d") || newName.ToLower().Contains("mob") || newName.ToLower().Contains("m0b"))
            return;
        if (!newName.ToLower().Contains("mod") && (habbo.Rank == 2 || habbo.Rank == 3))
            return;
        if (newName.Length > 15)
            return;
        if (newName.Length < 3)
            return;
        if (!_clientManager.UpdateClientUsername(session, oldName, newName))
        {
            session.SendNotification("Oops! An issue occoured whilst updating your username.");
            return;
        }
        habbo.ChangingName = false;
        await _roomService.LeaveRoom(session);
        habbo.ChangeName(_database, newName);
        habbo.Messenger?.NotifyChangesToFriends();
        session.Send(new UpdateUsernameComposer(newName));
        room.SendPacket(new UserNameChangeComposer(room.Id, user.VirtualId, newName));
        using (var connection = _database.Connection())
        {
            connection.Execute("INSERT INTO `logs_client_namechange` (`user_id`,`new_name`,`old_name`,`timestamp`) VALUES (@id,@new_name,@old_name,@timestamp)",
                    new { id = habbo.Id, new_name = newName, old_name = oldName, timestamp = UnixTimestamp.GetNow() });
        }
        foreach (var ownRooms in _roomManager.GetRooms().ToList())
        {
            if (ownRooms == null || ownRooms.OwnerId != habbo.Id || ownRooms.OwnerName == newName)
                continue;
            ownRooms.OwnerName = newName;
            ownRooms.SendPacket(new RoomInfoUpdatedComposer(ownRooms.Id));
        }
        await _achievementService.ProgressAchievement(session, "ACH_Name", 1);
        session.Send(new RoomForwardComposer(room.Id));
    }

    private static bool CanChangeName(Habbo habbo)
    {
        if (habbo.Rank == 1 && (habbo.LastNameChange == 0 || UnixTimestamp.GetNow() + 604800 > habbo.LastNameChange))
            return true;
        if (habbo.Rank == 1 && (habbo.LastNameChange == 0 || UnixTimestamp.GetNow() + 86400 > habbo.LastNameChange))
            return true;
        if (habbo.Rank == 1)
            return true;
        if (habbo.Permissions?.HasRight("mod_tool") == true)
            return true;
        return false;
    }
}
