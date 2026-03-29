using System.Drawing;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Communication.Packets.Outgoing.Rooms.Notifications;
using Plus.Core;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Items.Interactor;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Games.Freeze;
using Plus.HabboHotel.Rooms.Games.Teams;
using Plus.HabboHotel.Rooms.PathFinding;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.HabboHotel.Items;

public partial class Item
{
    public uint Id { get; set; }
    public uint OwnerId { get; set; }
    public uint RoomId { get; set; }
    public ItemDefinition Definition { get; set; } = null!;
    public IFurniObjectData ExtraData { get; set; } = FurniObjectData.Empty;
    public uint UniqueNumber { get; set; }
    public uint UniqueSeries { get; set; }
    public string WallCoordinates = string.Empty;

    public string LegacyDataString {
        get
        {
            if (ExtraData is LegacyDataFormat data)
                return data.Data;
            return string.Empty;
        }
        set
        {
            if (ExtraData is LegacyDataFormat data)
                data.Data = value;
        }
    }



    /// TODO @80O: Cleanup shit below
    public Room? Room { get; set; }
    private bool _updateNeeded;
    public string Figure = string.Empty;
    public FreezePowerUp FreezePowerUp;
    public string Gender = string.Empty;
    public int GroupId;
    public int InteractingBallUser;
    public int InteractingUser;
    public int InteractingUser2;
    public byte InteractionCount;
    public byte InteractionCountHelper;
    public bool MagicRemove = false;
    public bool PendingReset = false;
    public int Rotation;

    public Team Team;
    public int UpdateCounter;
    public int UserId;
    public string Username = string.Empty;


    public int Value;

    public Dictionary<int, ThreeDCoord> GetAffectedTiles { get; private set; } = new();

    public int GetX { get; set; }

    public int GetY { get; set; }

    public double GetZ { get; set; }

    public bool UpdateNeeded
    {
        get => _updateNeeded;
        set
        {
            if (value && GetRoom() != null)
                GetRoom().GetRoomItemHandler().QueueRoomItemUpdate(this);
            _updateNeeded = value;
        }
    }
    
    public bool IsRoller => Definition.IsRoller;

    public Point Coordinate => new(GetX, GetY);

    public List<Point> GetCoords
    {
        get
        {
            var toReturn = new List<Point>
            {
                Coordinate
            };
            foreach (var tile in GetAffectedTiles.Values) toReturn.Add(new(tile.X, tile.Y));
            return toReturn;
        }
    }

    public double TotalHeight
    {
        get
        {
            var curHeight = 0.0;
            if (Definition.AdjustableHeights.Count > 1)
            {
                if (int.TryParse(LegacyDataString, out var num2) && Definition.AdjustableHeights.Count - 1 >= num2)
                    curHeight = GetZ + Definition.AdjustableHeights[num2];
            }
            if (curHeight <= 0.0)
                curHeight = GetZ + Definition.Height;
            return curHeight;
        }
    }

    public bool IsWallItem => Definition.Type == ItemType.Wall;

    public bool IsFloorItem => Definition.Type == ItemType.Floor;

    public Point SquareInFront
    {
        get
        {
            var sq = new Point(GetX, GetY);
            if (Rotation == 0)
                sq.Y--;
            else if (Rotation == 2)
                sq.X++;
            else if (Rotation == 4)
                sq.Y++;
            else if (Rotation == 6) sq.X--;
            return sq;
        }
    }

    public Point SquareBehind
    {
        get
        {
            var sq = new Point(GetX, GetY);
            if (Rotation == 0)
                sq.Y++;
            else if (Rotation == 2)
                sq.X--;
            else if (Rotation == 4)
                sq.Y--;
            else if (Rotation == 6) sq.X++;
            return sq;
        }
    }

    public Point SquareLeft
    {
        get
        {
            var sq = new Point(GetX, GetY);
            if (Rotation == 0)
                sq.X++;
            else if (Rotation == 2)
                sq.Y--;
            else if (Rotation == 4)
                sq.X--;
            else if (Rotation == 6) sq.Y++;
            return sq;
        }
    }

    public Point SquareRight
    {
        get
        {
            var sq = new Point(GetX, GetY);
            if (Rotation == 0)
                sq.X--;
            else if (Rotation == 2)
                sq.Y++;
            else if (Rotation == 4)
                sq.X++;
            else if (Rotation == 6) sq.Y--;
            return sq;
        }
    }

    public IFurniInteractor Interactor
    {
        get
        {
            if (Definition.IsWired) return new InteractorWired();
            switch (Definition.InteractionType)
            {
                case var _ when Definition.IsGate:
                    return new InteractorGate();
                case var _ when Definition.IsTeleport:
                    return new InteractorTeleport();
                case var _ when Definition.IsHopper:
                    return new InteractorHopper();
                case var _ when Definition.IsBottle:
                    return new InteractorSpinningBottle();
                case var _ when Definition.IsDice:
                    return new InteractorDice();
                case var _ when Definition.IsHabboWheel:
                    return new InteractorHabboWheel();
                case var _ when Definition.IsLoveShuffler:
                    return new InteractorLoveShuffler();
                case var _ when Definition.IsOneWayGate:
                    return new InteractorOneWayGate();
                case var _ when Definition.IsAlert:
                    return new InteractorAlert();
                case var _ when Definition.IsVendingMachine:
                    return new InteractorVendor();
                case var _ when Definition.IsScoreboard:
                    return new InteractorScoreboard();
                case var _ when Definition.IsPuzzleBox:
                    return new InteractorPuzzleBox();
                case var _ when Definition.IsMannequin:
                    return new InteractorMannequin();
                case var _ when Definition.IsBanzaiCounter:
                    return new InteractorBanzaiTimer();
                case var _ when Definition.IsFreezeTimer:
                    return new InteractorFreezeTimer();
                case var _ when Definition.IsFreezeTile || Definition.IsFreezeTileBlock:
                    return new InteractorFreezeTile();
                case var _ when Definition.IsFootballCounter:
                    return new InteractorScoreCounter();
                case var _ when Definition.IsBanzaiScore:
                    return new InteractorBanzaiScoreCounter();
                case var _ when Definition.IsFloorSwitch:
                    return new InteractorSwitch();
                case var _ when Definition.IsLovelock:
                    return new InteractorLoveLock();
                case var _ when Definition.IsCannon:
                    return new InteractorCannon();
                case var _ when Definition.IsCounter:
                    return new InteractorCounter();
                case InteractionType.None:
                default:
                    return new InteractorGenericSwitch();
            }
        }
    }

    public bool IsWired
        => Definition.IsWired;

    public List<Point> GetSides()
    {
        var sides = new List<Point>
        {
            SquareBehind,
            SquareInFront,
            SquareLeft,
            SquareRight,
            Coordinate
        };
        return sides;
    }

    public void SetState(int pX, int pY, double pZ, Dictionary<int, ThreeDCoord> tiles)
    {
        GetX = pX;
        GetY = pY;
        if (!double.IsInfinity(pZ)) GetZ = pZ;
        GetAffectedTiles = tiles;
    }

    public void ProcessUpdates()
    {
        try
        {
            UpdateCounter--;
            if (UpdateCounter <= 0)
            {
                UpdateNeeded = false;
                UpdateCounter = 0;
                switch (Definition.InteractionType)
                {
                    case var _ when Definition.IsGroupGate:
                    {
                        if (LegacyDataString == "1")
                        {
                            if (GetRoom().GetRoomUserManager().GetUserForSquare(GetX, GetY) == null)
                            {
                                LegacyDataString = "0";
                                UpdateState(false, true);
                            }
                            else
                                RequestUpdate(2, false);
                        }
                        break;
                    }
                    case var _ when Definition.IsEffectProviderFurni:
                    {
                        if (LegacyDataString == "1")
                        {
                            if (GetRoom().GetRoomUserManager().GetUserForSquare(GetX, GetY) == null)
                            {
                                LegacyDataString = "0";
                                UpdateState(false, true);
                            }
                            else
                                RequestUpdate(2, false);
                        }
                        break;
                    }
                    case var _ when Definition.IsOneWayGate:
                        ProcessOneWayGateUpdate();
                        break;
                    case var _ when Definition.IsGateVip:
                        ProcessVipGateUpdate();
                        break;
                    case var _ when Definition.IsHopper:
                        ProcessHopperUpdate();
                        break;
                    case var _ when Definition.IsTeleport:
                        ProcessTeleportUpdate();
                        break;
                    case var _ when Definition.IsBottle:
                        ProcessBottleUpdate();
                        break;
                    case var _ when Definition.IsDice:
                        ProcessDiceUpdate();
                        break;
                    case var _ when Definition.IsHabboWheel:
                        ProcessHabboWheelUpdate();
                        break;
                    case var _ when Definition.IsLoveShuffler:
                        ProcessLoveShufflerUpdate();
                        break;
                    case var _ when Definition.IsAlert:
                        ProcessAlertUpdate();
                        break;
                    case var _ when Definition.IsVendingMachine:
                        ProcessVendingMachineUpdate();
                        break;
                    case var _ when Definition.IsScoreboard:
                        ProcessScoreboardUpdate();
                        break;
                    case var _ when Definition.IsBanzaiCounter:
                        ProcessBanzaiCounterUpdate();
                        break;
                    case var _ when Definition.IsBanzaiTeleport:
                    {
                        LegacyDataString = string.Empty;
                        UpdateState();
                        break;
                    }
                    case var _ when Definition.IsBanzaiFloor:
                        ProcessBanzaiFloorUpdate();
                        break;
                    case var _ when Definition.IsBanzaiPuck:
                        ProcessBanzaiPuckUpdate();
                        break;
                    case var _ when Definition.IsFreezeTile:
                        ProcessFreezeTileUpdate();
                        break;
                    case var _ when Definition.IsCounter:
                        ProcessCounterUpdate();
                        break;
                    case var _ when Definition.IsFreezeTimer:
                        ProcessFreezeTimerUpdate();
                        break;
                    case var _ when Definition.IsPressurePad:
                        ProcessPressurePadUpdate();
                        break;
                    case var _ when Definition.IsWired:
                        ProcessWiredResetUpdate();
                        break;
                    case var _ when Definition.IsCannon:
                        ProcessCannonUpdate();
                        break;
                }
            }
        }
        catch (Exception e)
        {
            ExceptionLogger.LogException(e);
        }
    }

    public static string[] RandomizeStrings(string[] arr)
    {
        var list = new List<KeyValuePair<int, string>>();
        // Add all strings from array
        // Add new random int each time
        foreach (var s in arr) list.Add(new(Random.Shared.Next(), s));
        // Sort the list by the random number
        var sorted = from item in list
            orderby item.Key
            select item;
        // Allocate new string array
        var result = new string[arr.Length];
        // Copy values to array
        var index = 0;
        foreach (var pair in sorted)
        {
            result[index] = pair.Value;
            index++;
        }
        // Return copied array
        return result;
    }

    public void RequestUpdate(int cycles, bool setUpdate)
    {
        UpdateCounter = cycles;
        if (setUpdate)
            UpdateNeeded = true;
    }

    public void UpdateState()
    {
        UpdateState(true, true);
    }

    public void UpdateState(bool inDb, bool inRoom)
    {
        if (GetRoom() == null)
            return;
        if (inDb)
            GetRoom().GetRoomItemHandler().UpdateItem(this);
        if (inRoom)
        {
            if (IsFloorItem)
                GetRoom().SendPacket(new ObjectUpdateComposer(this));
            else
                GetRoom().SendPacket(new ItemUpdateComposer(this));
        }
    }

    public Room GetRoom()
    {
        if (Room != null)
            return Room;
        return null!;
    }

    public void UserFurniCollision(RoomUser user)
    {
        var habbo = GetHabbo(user);
        if (habbo == null)
            return;
        GetRoom().GetWired().TriggerEvent(WiredBoxType.TriggerUserFurniCollision, habbo, this);
    }

    public void UserWalksOnFurni(RoomUser user)
    {
        var habbo = GetHabbo(user);
        if (habbo == null)
            return;
        if (Definition.IsTent) GetRoom().AddUserToTent(Id, user);
        GetRoom().GetWired().TriggerEvent(WiredBoxType.TriggerWalkOnFurni, habbo, this);
        user.LastItem = this;
    }

    public void UserWalksOffFurni(RoomUser user)
    {
        var habbo = GetHabbo(user);
        if (habbo == null)
            return;
        if (Definition.IsTent)
            GetRoom().RemoveUserFromTent(Id, user);
        GetRoom().GetWired().TriggerEvent(WiredBoxType.TriggerWalkOffFurni, habbo, this);
    }

    private Habbo? GetHabbo(RoomUser? user)
    {
        var client = user?.GetClient();
        return client?.GetHabbo();
    }

    public void Destroy()
    {
        Room = null;
        Definition = null!;
        GetAffectedTiles.Clear();
    }
}
