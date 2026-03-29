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

    private void ProcessBottleUpdate()
    {
        LegacyDataString = Random.Shared.Next(0, 8).ToString();
        UpdateState();
    }

    private void ProcessDiceUpdate()
    {
        if (LegacyDataString == "-1")
            LegacyDataString = RandomizeStrings(new[] { "1", "2", "3", "4", "5", "6" })[0];

        UpdateState();
    }

    private void ProcessHabboWheelUpdate()
    {
        LegacyDataString = Random.Shared.Next(1, 10).ToString();
        UpdateState();
    }

    private void ProcessLoveShufflerUpdate()
    {
        if (LegacyDataString == "0")
        {
            LegacyDataString = Random.Shared.Next(1, 5).ToString();
            RequestUpdate(20, false);
        }
        else if (LegacyDataString != "-1")
        {
            LegacyDataString = "-1";
        }

        UpdateState(false, true);
    }

    private void ProcessAlertUpdate()
    {
        if (LegacyDataString != "1")
            return;

        LegacyDataString = "0";
        UpdateState(false, true);
    }

    private void ProcessVendingMachineUpdate()
    {
        if (LegacyDataString != "1")
            return;

        var user = GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser);
        if (user == null)
            return;

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

    private void ProcessOneWayGateUpdate()
    {
        var user = InteractingUser > 0
            ? GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser)
            : null;

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
        else
        {
            if (LegacyDataString == "1")
            {
                LegacyDataString = "0";
                UpdateState(false, true);
            }

            if (user == null)
                InteractingUser = 0;
        }
    }

    private void ProcessVipGateUpdate()
    {
        var user = InteractingUser > 0
            ? GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser)
            : null;

        var deltaY = 0;
        var deltaX = 0;

        if (user != null && user.X == GetX && user.Y == GetY)
        {
            if (user.RotBody == 4)
                deltaY = 1;
            else if (user.RotBody == 0)
                deltaY = -1;
            else if (user.RotBody == 6)
                deltaX = -1;
            else if (user.RotBody == 2)
                deltaX = 1;

            user.MoveTo(user.X + deltaX, user.Y + deltaY);
            RequestUpdate(1, false);
        }
        else if (user != null && (user.Coordinate == SquareBehind || user.Coordinate == SquareInFront))
        {
            user.UnlockWalking();
            LegacyDataString = "0";
            InteractingUser = 0;
            UpdateState(false, true);
        }
        else
        {
            if (LegacyDataString == "1")
            {
                LegacyDataString = "0";
                UpdateState(false, true);
            }

            if (user == null)
                InteractingUser = 0;
        }
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

    private int ParseLegacySeconds()
    {
        if (string.IsNullOrEmpty(LegacyDataString))
            return 0;

        try
        {
            return int.Parse(LegacyDataString);
        }
        catch
        {
            return 0;
        }
    }

    private void ProcessScoreboardUpdate()
    {
        if (string.IsNullOrEmpty(LegacyDataString))
            return;

        var seconds = ParseLegacySeconds();
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
            {
                InteractionCountHelper++;
            }

            UpdateCounter = 1;
        }
        else
        {
            UpdateCounter = 0;
        }
    }

    private void ProcessBanzaiCounterUpdate()
    {
        if (string.IsNullOrEmpty(LegacyDataString))
            return;

        var seconds = ParseLegacySeconds();
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
                {
                    return;
                }
            }
            else
            {
                InteractionCountHelper++;
            }

            UpdateCounter = 1;
            return;
        }

        UpdateCounter = 0;
        GetRoom().GetBanzai().BanzaiEnd();
    }

    private void ProcessBanzaiFloorUpdate()
    {
        if (Value != 3)
            return;

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
        UpdateCounter = InteractionCount < 16 ? 1 : 0;
    }

    private void ProcessBanzaiPuckUpdate()
    {
        if (InteractionCount > 4)
        {
            InteractionCount++;
            UpdateCounter = 1;
            return;
        }

        InteractionCount = 0;
        UpdateCounter = 0;
    }

    private void ProcessFreezeTileUpdate()
    {
        if (InteractingUser <= 0)
            return;

        LegacyDataString = "11000";
        UpdateState(false, true);
        GetRoom().GetFreeze().OnFreezeTiles(this, FreezePowerUp);
        InteractingUser = 0;
        InteractionCountHelper = 0;
    }

    private void ProcessCounterUpdate()
    {
        if (string.IsNullOrEmpty(LegacyDataString))
            return;

        var seconds = ParseLegacySeconds();
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
                {
                    return;
                }
            }
            else
            {
                InteractionCountHelper++;
            }

            UpdateCounter = 1;
        }
        else
        {
            UpdateNeeded = false;
            GetRoom().GetSoccer().StopGame();
        }
    }

    private void ProcessFreezeTimerUpdate()
    {
        if (string.IsNullOrEmpty(LegacyDataString))
            return;

        var seconds = ParseLegacySeconds();
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
                {
                    return;
                }
            }
            else
            {
                InteractionCountHelper++;
            }

            UpdateCounter = 1;
        }
        else
        {
            UpdateNeeded = false;
            GetRoom().GetFreeze().StopGame();
        }
    }

    private void ProcessPressurePadUpdate()
    {
        LegacyDataString = "1";
        UpdateState();
    }

    private void ProcessWiredResetUpdate()
    {
        if (LegacyDataString != "1")
            return;

        LegacyDataString = "0";
        UpdateState(false, true);
    }

    private void ProcessCannonUpdate()
    {
        if (LegacyDataString != "1")
            return;

        var room = GetRoom();
        foreach (var square in GetCannonTargetSquares())
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

        LegacyDataString = "2";
        UpdateState(false, true);
    }

    private List<Point> GetCannonTargetSquares()
    {
        var targetStart = Coordinate;
        var targetSquares = new List<Point>();

        switch (Rotation)
        {
            case 0:
                targetStart = new(GetX - 1, GetY);
                break;
            case 2:
                targetStart = new(GetX, GetY - 1);
                break;
            case 4:
                targetStart = new(GetX + 2, GetY);
                break;
            case 6:
                targetStart = new(GetX, GetY + 2);
                break;
        }

        if (!targetSquares.Contains(targetStart))
            targetSquares.Add(targetStart);

        for (var offset = 1; offset <= 3; offset++)
        {
            Point targetSquare = Rotation switch
            {
                0 => new(targetStart.X - offset, targetStart.Y),
                2 => new(targetStart.X, targetStart.Y - offset),
                4 => new(targetStart.X + offset, targetStart.Y),
                6 => new(targetStart.X, targetStart.Y + offset),
                _ => targetStart
            };

            if (!targetSquares.Contains(targetSquare))
                targetSquares.Add(targetSquare);
        }

        return targetSquares;
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
