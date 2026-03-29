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

public class Item
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
                RoomUser? user = null;
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
                        user = null;
                        if (InteractingUser > 0) user = GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser);
                        if (user != null && user.X == GetX && user.Y == GetY)
                        {
                            LegacyDataString = "1";
                            user.MoveTo(SquareBehind);
                            user.InteractingGate = false;
                            user.GateId = 0;
                            RequestUpdate(1, false);
                            UpdateState(false, true);
                        }
                        else if (user != null && user.Coordinate == SquareBehind)
                        {
                            user.UnlockWalking();
                            LegacyDataString = "0";
                            InteractingUser = 0;
                            user.InteractingGate = false;
                            user.GateId = 0;
                            UpdateState(false, true);
                        }
                        else if (LegacyDataString == "1")
                        {
                            LegacyDataString = "0";
                            UpdateState(false, true);
                        }
                        if (user == null) InteractingUser = 0;
                        break;
                    case var _ when Definition.IsGateVip:
                        user = null;
                        if (InteractingUser > 0) user = GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser);
                        var newY = 0;
                        var newX = 0;
                        if (user != null && user.X == GetX && user.Y == GetY)
                        {
                            if (user.RotBody == 4)
                                newY = 1;
                            else if (user.RotBody == 0)
                                newY = -1;
                            else if (user.RotBody == 6)
                                newX = -1;
                            else if (user.RotBody == 2) newX = 1;
                            user.MoveTo(user.X + newX, user.Y + newY);
                            RequestUpdate(1, false);
                        }
                        else if (user != null && (user.Coordinate == SquareBehind || user.Coordinate == SquareInFront))
                        {
                            user.UnlockWalking();
                            LegacyDataString = "0";
                            InteractingUser = 0;
                            UpdateState(false, true);
                        }
                        else if (LegacyDataString == "1")
                        {
                            LegacyDataString = "0";
                            UpdateState(false, true);
                        }
                        if (user == null) InteractingUser = 0;
                        break;
                    case var _ when Definition.IsHopper:
                        ProcessHopperUpdate();
                        break;
                    case var _ when Definition.IsTeleport:
                        ProcessTeleportUpdate();
                        break;
                    case var _ when Definition.IsBottle:
                        LegacyDataString = Random.Shared.Next(0, 8).ToString();
                        UpdateState();
                        break;
                    case var _ when Definition.IsDice:
                    {
                        var numbers = new[] { "1", "2", "3", "4", "5", "6" };
                        if (LegacyDataString == "-1")
                            LegacyDataString = RandomizeStrings(numbers)[0];
                        UpdateState();
                    }
                        break;
                    case var _ when Definition.IsHabboWheel:
                        LegacyDataString = Random.Shared.Next(1, 10).ToString();
                        UpdateState();
                        break;
                    case var _ when Definition.IsLoveShuffler:
                        if (LegacyDataString == "0")
                        {
                            LegacyDataString = Random.Shared.Next(1, 5).ToString();
                            RequestUpdate(20, false);
                        }
                        else if (LegacyDataString != "-1") LegacyDataString = "-1";
                        UpdateState(false, true);
                        break;
                    case var _ when Definition.IsAlert:
                        if (LegacyDataString == "1")
                        {
                            LegacyDataString = "0";
                            UpdateState(false, true);
                        }
                        break;
                    case var _ when Definition.IsVendingMachine:
                        if (LegacyDataString == "1")
                        {
                            user = GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser);
                            if (user == null)
                                break;
                            user.UnlockWalking();
                            if (Definition.VendingIds.Count > 0)
                            {
                                var randomDrink = Definition.VendingIds[Random.Shared.Next(0, Definition.VendingIds.Count)];
                                user.CarryItem(randomDrink);
                            }
                            InteractingUser = 0;
                            LegacyDataString = "0";
                            UpdateState(false, true);
                        }
                        break;
                    case var _ when Definition.IsScoreboard:
                    {
                        if (string.IsNullOrEmpty(LegacyDataString))
                            break;
                        var seconds = 0;
                        try
                        {
                            seconds = int.Parse(LegacyDataString);
                        }
                        catch { }
                        if (seconds > 0)
                        {
                            if (InteractionCountHelper == 1)
                            {
                                seconds--;
                                InteractionCountHelper = 0;
                                LegacyDataString = seconds.ToString();
                                UpdateState();
                            }
                            else
                                InteractionCountHelper++;
                            UpdateCounter = 1;
                        }
                        else
                            UpdateCounter = 0;
                        break;
                    }
                    case var _ when Definition.IsBanzaiCounter:
                    {
                        if (string.IsNullOrEmpty(LegacyDataString))
                            break;
                        var seconds = 0;
                        try
                        {
                            seconds = int.Parse(LegacyDataString);
                        }
                        catch { }
                        if (seconds > 0)
                        {
                            if (InteractionCountHelper == 1)
                            {
                                seconds--;
                                InteractionCountHelper = 0;
                                if (GetRoom().GetBanzai().IsBanzaiActive)
                                {
                                    LegacyDataString = seconds.ToString();
                                    UpdateState();
                                }
                                else
                                    break;
                            }
                            else
                                InteractionCountHelper++;
                            UpdateCounter = 1;
                        }
                        else
                        {
                            UpdateCounter = 0;
                            GetRoom().GetBanzai().BanzaiEnd();
                        }
                        break;
                    }
                    case var _ when Definition.IsBanzaiTeleport:
                    {
                        LegacyDataString = string.Empty;
                        UpdateState();
                        break;
                    }
                    case var _ when Definition.IsBanzaiFloor:
                    {
                        if (Value == 3)
                        {
                            if (InteractionCountHelper == 1)
                            {
                                InteractionCountHelper = 0;
                                LegacyDataString = Definition.GetBanzaiFloorPulseState(Team);
                            }
                            else
                            {
                                LegacyDataString = "";
                                InteractionCountHelper++;
                            }
                            UpdateState();
                            InteractionCount++;
                            if (InteractionCount < 16)
                                UpdateCounter = 1;
                            else
                                UpdateCounter = 0;
                        }
                        break;
                    }
                    case var _ when Definition.IsBanzaiPuck:
                    {
                        if (InteractionCount > 4)
                        {
                            InteractionCount++;
                            UpdateCounter = 1;
                        }
                        else
                        {
                            InteractionCount = 0;
                            UpdateCounter = 0;
                        }
                        break;
                    }
                    case var _ when Definition.IsFreezeTile:
                    {
                        if (InteractingUser > 0)
                        {
                            LegacyDataString = "11000";
                            UpdateState(false, true);
                            GetRoom().GetFreeze().OnFreezeTiles(this, FreezePowerUp);
                            InteractingUser = 0;
                            InteractionCountHelper = 0;
                        }
                        break;
                    }
                    case var _ when Definition.IsCounter:
                    {
                        if (string.IsNullOrEmpty(LegacyDataString))
                            break;
                        var seconds = 0;
                        try
                        {
                            seconds = int.Parse(LegacyDataString);
                        }
                        catch { }
                        if (seconds > 0)
                        {
                            if (InteractionCountHelper == 1)
                            {
                                seconds--;
                                InteractionCountHelper = 0;
                                if (GetRoom().GetSoccer().GameIsStarted)
                                {
                                    LegacyDataString = seconds.ToString();
                                    UpdateState();
                                }
                                else
                                    break;
                            }
                            else
                                InteractionCountHelper++;
                            UpdateCounter = 1;
                        }
                        else
                        {
                            UpdateNeeded = false;
                            GetRoom().GetSoccer().StopGame();
                        }
                        break;
                    }
                    case var _ when Definition.IsFreezeTimer:
                    {
                        if (string.IsNullOrEmpty(LegacyDataString))
                            break;
                        var seconds = 0;
                        try
                        {
                            seconds = int.Parse(LegacyDataString);
                        }
                        catch { }
                        if (seconds > 0)
                        {
                            if (InteractionCountHelper == 1)
                            {
                                seconds--;
                                InteractionCountHelper = 0;
                                if (GetRoom().GetFreeze().GameIsStarted)
                                {
                                    LegacyDataString = seconds.ToString();
                                    UpdateState();
                                }
                                else
                                    break;
                            }
                            else
                                InteractionCountHelper++;
                            UpdateCounter = 1;
                        }
                        else
                        {
                            UpdateNeeded = false;
                            GetRoom().GetFreeze().StopGame();
                        }
                        break;
                    }
                    case var _ when Definition.IsPressurePad:
                    {
                        LegacyDataString = "1";
                        UpdateState();
                        break;
                    }
                    case var _ when Definition.IsWired:
                    {
                        if (LegacyDataString == "1")
                        {
                            LegacyDataString = "0";
                            UpdateState(false, true);
                        }
                    }
                        break;
                    case var _ when Definition.IsCannon:
                    {
                        if (LegacyDataString != "1")
                            break;
                        var targetStart = Coordinate;
                        var targetSquares = new List<Point>();
                        switch (Rotation)
                        {
                            case 0:
                            {
                                targetStart = new(GetX - 1, GetY);
                                if (!targetSquares.Contains(targetStart))
                                    targetSquares.Add(targetStart);
                                for (var I = 1; I <= 3; I++)
                                {
                                    var targetSquare = new Point(targetStart.X - I, targetStart.Y);
                                    if (!targetSquares.Contains(targetSquare))
                                        targetSquares.Add(targetSquare);
                                }
                                break;
                            }
                            case 2:
                            {
                                targetStart = new(GetX, GetY - 1);
                                if (!targetSquares.Contains(targetStart))
                                    targetSquares.Add(targetStart);
                                for (var I = 1; I <= 3; I++)
                                {
                                    var targetSquare = new Point(targetStart.X, targetStart.Y - I);
                                    if (!targetSquares.Contains(targetSquare))
                                        targetSquares.Add(targetSquare);
                                }
                                break;
                            }
                            case 4:
                            {
                                targetStart = new(GetX + 2, GetY);
                                if (!targetSquares.Contains(targetStart))
                                    targetSquares.Add(targetStart);
                                for (var I = 1; I <= 3; I++)
                                {
                                    var targetSquare = new Point(targetStart.X + I, targetStart.Y);
                                    if (!targetSquares.Contains(targetSquare))
                                        targetSquares.Add(targetSquare);
                                }
                                break;
                            }
                            case 6:
                            {
                                targetStart = new(GetX, GetY + 2);
                                if (!targetSquares.Contains(targetStart))
                                    targetSquares.Add(targetStart);
                                for (var I = 1; I <= 3; I++)
                                {
                                    var targetSquare = new Point(targetStart.X, targetStart.Y + I);
                                    if (!targetSquares.Contains(targetSquare))
                                        targetSquares.Add(targetSquare);
                                }
                                break;
                            }
                        }
                        if (targetSquares.Count > 0)
                        {
                            var room = GetRoom();
                            foreach (var square in targetSquares.ToList())
                            {
                                var affectedUsers = room.GetGameMap().GetRoomUsers(square).ToList();
                                if (affectedUsers.Count == 0)
                                    continue;
                                foreach (var target in affectedUsers)
                                {
                                    if (target == null || target.IsBot || target.IsPet)
                                        continue;
                                    var targetClient = target.GetClient();
                                    var targetHabbo = GetHabbo(target);
                                    if (targetHabbo == null || targetClient == null)
                                        continue;
                                    if (room.CheckRights(targetClient, true))
                                        continue;
                                    target.ApplyEffect(4);
                                    targetClient.Send(new RoomNotificationComposer("Kicked from room", "You were hit by a cannonball!", "room_kick_cannonball", ""));
                                    target.ApplyEffect(0);
                                    _ = room.GetRoomService().LeaveRoom(targetClient);
                                }
                            }
                        }
                        LegacyDataString = "2";
                        UpdateState(false, true);
                    }
                        break;
                }
            }
        }
        catch (Exception e)
        {
            ExceptionLogger.LogException(e);
        }
    }

    private void ProcessHopperUpdate()
    {
        RoomUser? user = null;
        RoomUser? user2 = null;
        var showHopperEffect = false;
        var keepDoorOpen = false;
        var pause = 0;

        if (InteractingUser > 0)
        {
            user = GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser);

            if (user != null)
            {
                if (user.Coordinate == Coordinate)
                {
                    user.AllowOverride = false;
                    if (user.TeleDelay == 0)
                    {
                        var roomHopId = GetRoom().GetItemHopperFinder().GetAHopper(user.RoomId);
                        var nextHopperId = GetRoom().GetItemHopperFinder().GetHopperId(roomHopId);
                        var habbo = GetHabbo(user);
                        if (habbo != null)
                        {
                            habbo.IsHopping = true;
                            habbo.HopperId = nextHopperId;
                            _ = GetRoom().GetRoomService().PrepareRoom(user.GetClient()!, roomHopId, "");
                            InteractingUser = 0;
                        }
                    }
                    else
                    {
                        user.TeleDelay--;
                        showHopperEffect = true;
                    }
                }
                else if (user.Coordinate == SquareInFront)
                {
                    user.AllowOverride = true;
                    keepDoorOpen = true;

                    if (user.IsWalking && (user.GoalX != GetX || user.GoalY != GetY))
                        user.ClearMovement(true);

                    user.CanWalk = false;
                    user.AllowOverride = true;
                    user.MoveTo(Coordinate.X, Coordinate.Y, true);
                }
                else
                {
                    InteractingUser = 0;
                }
            }
            else
            {
                InteractingUser = 0;
            }
        }

        if (InteractingUser2 > 0)
        {
            user2 = GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser2);
            if (user2 != null)
            {
                keepDoorOpen = true;
                user2.UnlockWalking();
                user2.MoveTo(SquareInFront);
            }

            InteractingUser2 = 0;
        }

        if (keepDoorOpen)
        {
            if (LegacyDataString != "1")
            {
                LegacyDataString = "1";
                UpdateState(false, true);
            }
        }
        else if (showHopperEffect)
        {
            if (LegacyDataString != "2")
            {
                LegacyDataString = "2";
                UpdateState(false, true);
            }
        }
        else if (LegacyDataString != "0")
        {
            if (pause == 0)
            {
                LegacyDataString = "0";
                UpdateState(false, true);
                pause = 2;
            }
            else
            {
                pause--;
            }
        }

        RequestUpdate(1, false);
    }

    private void ProcessTeleportUpdate()
    {
        RoomUser? user = null;
        RoomUser? user2 = null;
        var keepDoorOpen = false;
        var showTeleEffect = false;

        if (InteractingUser > 0)
        {
            user = GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser);

            if (user != null)
            {
                if (user.Coordinate == Coordinate)
                {
                    user.AllowOverride = false;
                    if (GetRoom().GetItemTeleporterFinder().IsTeleLinked(Id, GetRoom()))
                    {
                        showTeleEffect = true;
                        var teleId = GetRoom().GetItemTeleporterFinder().GetLinkedTele(Id);
                        var roomId = GetRoom().GetItemTeleporterFinder().GetTeleRoomId(teleId, GetRoom());

                        if (roomId == RoomId)
                        {
                            var item = GetRoom().GetRoomItemHandler().GetItem(teleId);
                            if (item == null)
                            {
                                user.UnlockWalking();
                            }
                            else
                            {
                                user.SetPos(item.GetX, item.GetY, item.GetZ);
                                user.SetRot(item.Rotation, false);
                                item.LegacyDataString = "2";
                                item.UpdateState(false, true);
                                item.InteractingUser2 = InteractingUser;
                                GetRoom().GetGameMap().RemoveUserFromMap(user, new(GetX, GetY));
                                InteractingUser = 0;
                            }
                        }
                        else if (user.TeleDelay == 0)
                        {
                            var habbo = GetHabbo(user);
                            if (habbo != null)
                            {
                                habbo.IsTeleporting = true;
                                habbo.TeleportingRoomId = roomId;
                                habbo.TeleporterId = teleId;
                                _ = GetRoom().GetRoomService().PrepareRoom(user.GetClient()!, roomId, "");
                                InteractingUser = 0;
                            }
                        }
                        else
                        {
                            user.TeleDelay--;
                            showTeleEffect = true;
                        }

                        GetRoom().GetGameMap().GenerateMaps();
                    }
                    else
                    {
                        user.UnlockWalking();
                        InteractingUser = 0;
                    }
                }
                else if (user.Coordinate == SquareInFront)
                {
                    user.AllowOverride = true;
                    keepDoorOpen = true;

                    if (user.IsWalking && (user.GoalX != GetX || user.GoalY != GetY))
                        user.ClearMovement(true);

                    user.CanWalk = false;
                    user.AllowOverride = true;
                    user.MoveTo(Coordinate.X, Coordinate.Y, true);
                }
                else
                {
                    InteractingUser = 0;
                }
            }
            else
            {
                InteractingUser = 0;
            }
        }

        if (InteractingUser2 > 0)
        {
            user2 = GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser2);
            if (user2 != null)
            {
                keepDoorOpen = true;
                user2.UnlockWalking();
                user2.MoveTo(SquareInFront);
            }

            InteractingUser2 = 0;
        }

        if (showTeleEffect)
        {
            if (LegacyDataString != "2")
            {
                LegacyDataString = "2";
                UpdateState(false, true);
            }
        }
        else if (keepDoorOpen)
        {
            if (LegacyDataString != "1")
            {
                LegacyDataString = "1";
                UpdateState(false, true);
            }
        }
        else if (LegacyDataString != "0")
        {
            LegacyDataString = "0";
            UpdateState(false, true);
        }

        RequestUpdate(1, false);
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
