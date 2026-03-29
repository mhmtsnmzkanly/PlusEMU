using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms.Games.Teams;

public class TeamManager
{
    public List<RoomUser> BlueTeam = [];
    public string Game = string.Empty;
    public List<RoomUser> GreenTeam = [];
    public List<RoomUser> RedTeam = [];
    public List<RoomUser> YellowTeam = [];

    public static TeamManager CreateTeam(string game)
    {
        var t = new TeamManager();
        t.Game = game;
        t.BlueTeam = new();
        t.RedTeam = new();
        t.GreenTeam = new();
        t.YellowTeam = new();
        return t;
    }

    public bool CanEnterOnTeam(Team t)
    {
        if (t.Equals(Team.Blue))
            return BlueTeam.Count < 5;
        if (t.Equals(Team.Red))
            return RedTeam.Count < 5;
        if (t.Equals(Team.Yellow))
            return YellowTeam.Count < 5;
        if (t.Equals(Team.Green))
            return GreenTeam.Count < 5;
        return false;
    }

    public void AddUser(RoomUser user)
    {
        if (user.Team.Equals(Team.Blue) && !BlueTeam.Contains(user))
            BlueTeam.Add(user);
        else if (user.Team.Equals(Team.Red) && !RedTeam.Contains(user))
            RedTeam.Add(user);
        else if (user.Team.Equals(Team.Yellow) && !YellowTeam.Contains(user))
            YellowTeam.Add(user);
        else if (user.Team.Equals(Team.Green) && !GreenTeam.Contains(user))
            GreenTeam.Add(user);
        if (!TryGetRoom(user, out var room))
            return;

        switch (Game.ToLower())
        {
            case "banzai":
            {
                UpdateBanzaiGateCounts(room, lockFullGates: true);
                break;
            }
            case "freeze":
            {
                UpdateFreezeGateCounts(room);
                break;
            }
        }
    }

    public void OnUserLeave(RoomUser user)
    {
        //Console.WriteLine("remove user from team! (" + Game + ")");
        if (user.Team.Equals(Team.Blue) && BlueTeam.Contains(user))
            BlueTeam.Remove(user);
        else if (user.Team.Equals(Team.Red) && RedTeam.Contains(user))
            RedTeam.Remove(user);
        else if (user.Team.Equals(Team.Yellow) && YellowTeam.Contains(user))
            YellowTeam.Remove(user);
        else if (user.Team.Equals(Team.Green) && GreenTeam.Contains(user))
            GreenTeam.Remove(user);
        if (!TryGetRoom(user, out var room))
            return;

        switch (Game.ToLower())
        {
            case "banzai":
            {
                UpdateBanzaiGateCounts(room, lockFullGates: false);
                break;
            }
            case "freeze":
            {
                UpdateFreezeGateCounts(room);
                break;
            }
        }
    }

    private void UpdateBanzaiGateCounts(Room room, bool lockFullGates)
    {
        foreach (var item in room.GetRoomItemHandler().GetFloor.ToList())
        {
            if (item == null || !TryGetBanzaiGateTeam(item, out var team))
                continue;

            item.LegacyDataString = GetTeamCount(team).ToString();
            item.UpdateState();

            if (lockFullGates)
            {
                if (GetTeamCount(team) == 5)
                    SetGateWalkState(room, item, canWalk: false);
            }
            else if (room.GetGameMap().GameMap[item.GetX, item.GetY] == 0)
            {
                SetGateWalkState(room, item, canWalk: true);
            }
        }
    }

    private void UpdateFreezeGateCounts(Room room)
    {
        foreach (var item in room.GetRoomItemHandler().GetFloor.ToList())
        {
            if (item == null || !TryGetFreezeGateTeam(item, out var team))
                continue;

            item.LegacyDataString = GetTeamCount(team).ToString();
            item.UpdateState();
        }
    }

    private int GetTeamCount(Team team)
    {
        return team switch
        {
            Team.Blue => BlueTeam.Count,
            Team.Red => RedTeam.Count,
            Team.Yellow => YellowTeam.Count,
            Team.Green => GreenTeam.Count,
            _ => 0
        };
    }

    private static void SetGateWalkState(Room room, Item item, bool canWalk)
    {
        var walkState = (byte)(canWalk ? 1 : 0);
        foreach (var roomUser in room.GetGameMap().GetRoomUsers(new(item.GetX, item.GetY)))
            roomUser.SqState = walkState;

        room.GetGameMap().GameMap[item.GetX, item.GetY] = walkState;
    }

    private static bool TryGetBanzaiGateTeam(Item item, out Team team)
    {
        team = item.Definition.IsBanzaiGate
            ? item.Definition.GetTeamOrNone()
            : Team.None;

        return team != Team.None;
    }

    private static bool TryGetFreezeGateTeam(Item item, out Team team)
    {
        team = item.Definition.IsFreezeGate
            ? item.Definition.GetTeamOrNone()
            : Team.None;

        return team != Team.None;
    }

    private static bool TryGetRoom(RoomUser user, out Room room)
    {
        room = null!;
        var habbo = user.GetClient()?.GetHabbo();
        return habbo != null && habbo.TryGetCurrentRoom(out room);
    }

    public void Dispose()
    {
        BlueTeam.Clear();
        GreenTeam.Clear();
        RedTeam.Clear();
        YellowTeam.Clear();
    }
}
