using System.Collections.Concurrent;
using System.Drawing;
using Dapper;
using Plus.Communication.Packets;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Core;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms.PathFinding;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.HabboHotel.Rooms;

[Obsolete("Everything in here is bad and whoever wrote this must've been high on some crack or something")]
public class RoomItemHandling
{
    private const string DefaultWallPosition = ":w=0,2 l=11,53 l";

    private sealed class RollerTargetState
    {
        public bool NextSquareIsRoller { get; init; }
        public bool NextRollerClear { get; init; }
        public double NextRollerHeight { get; init; }
    }

    private readonly ConcurrentDictionary<uint, Item> _floorItems;
    private readonly ConcurrentDictionary<uint, Item> _movedItems;
    private readonly List<uint> _rollerItemsMoved;
    private readonly List<IServerPacket> _rollerMessages;

    private readonly ConcurrentDictionary<uint, Item> _rollers;
    private readonly List<int> _rollerUsersMoved;
    private readonly Room _room;
    private readonly ConcurrentDictionary<uint, Item> _wallItems;
    private readonly IItemLoader _itemLoader;
    private int _mRollerCycle;
    private int _mRollerSpeed;

    private ConcurrentQueue<Item> _roomItemUpdateQueue;

    public int HopperCount;
    public bool GotRollers { get; set; }

    public RoomItemHandling(Room room, IItemLoader itemLoader)
    {
        _room = room;
        _itemLoader = itemLoader;
        HopperCount = 0;
        GotRollers = false;
        _mRollerSpeed = 4;
        _mRollerCycle = 0;
        _movedItems = new();
        _rollers = new();
        _wallItems = new();
        _floorItems = new();
        _rollerItemsMoved = new();
        _rollerUsersMoved = new();
        _rollerMessages = new();
        _roomItemUpdateQueue = new();
    }

    public ICollection<Item> GetFloor => _floorItems.Values;
    public ICollection<Item> GetWall => _wallItems.Values;
    public IEnumerable<Item> GetWallAndFloor => _floorItems.Values.Concat(_wallItems.Values);

    public void TryAddRoller(uint itemId, Item roller)
    {
        _rollers.TryAdd(itemId, roller);
    }

    public void QueueRoomItemUpdate(Item item)
    {
        _roomItemUpdateQueue.Enqueue(item);
    }

    public void SetSpeed(int speed)
    {
        _mRollerSpeed = speed;
    }

    public string? WallPositionCheck(string wallPosition)
    {
        //:w=3,2 l=9,63 l
        try
        {
            if (wallPosition.Contains(Convert.ToChar(13))) return null;
            if (wallPosition.Contains(Convert.ToChar(9))) return null;
            var posD = wallPosition.Split(' ');
            if (posD[2] != "l" && posD[2] != "r")
                return null;
            if (!TryParseWallPair(posD[0].Substring(3), -1000, -1, out var widthX, out var widthY))
                return null;
            if (!TryParseWallPair(posD[1].Substring(2), -1, -1000, out var lengthX, out var lengthY))
                return null;
            return $":w={widthX},{widthY} l={lengthX},{lengthY} {posD[2]}";
        }
        catch
        {
            return null;
        }
    }

    private static bool TryParseWallPair(string rawPair, int minX, int minY, out int first, out int second)
    {
        first = 0;
        second = 0;

        var parts = rawPair.Split(',');
        if (parts.Length != 2)
            return false;

        first = int.Parse(parts[0]);
        second = int.Parse(parts[1]);
        return first >= minX && second >= minY && first <= 700 && second <= 700;
    }

    public void LoadFurniture()
    {
        ResetLoadedFurnitureState();
        var items = _itemLoader.GetItemsForRoom(_room.Id, _room);
        foreach (var item in items.ToList())
        {
            if (item == null)
                continue;

            EnsureOwnedItemUser(item);

            if (item.IsFloorItem)
            {
                if (TryRecoverInvalidFloorItem(item))
                    continue;

                RegisterLoadedItem(item);
            }
            else if (item.IsWallItem)
            {
                NormalizeWallItemPosition(item);
                RegisterLoadedItem(item);
            }
        }
        InitializeLoadedFloorItemState();
    }

    private void ResetLoadedFurnitureState()
    {
        if (_floorItems.Count > 0)
            _floorItems.Clear();
        if (_wallItems.Count > 0)
            _wallItems.Clear();
    }

    private void EnsureOwnedItemUser(Item item)
    {
        if (item.UserId != 0)
            return;

        using var connection = _room.GetDatabase().Connection();
        connection.Execute(
            "UPDATE `items` SET `user_id` = @userId WHERE `id` = @itemId LIMIT 1",
            new { itemId = item.Id, userId = _room.OwnerId });
    }

    private bool TryRecoverInvalidFloorItem(Item item)
    {
        if (_room.GetGameMap().ValidTile(item.GetX, item.GetY))
            return false;

        using (var connection = _room.GetDatabase().Connection())
            connection.Execute(
                "UPDATE `items` SET `room_id` = 0 WHERE `id` = @id LIMIT 1",
                new { id = item.Id });

        var client = _room.GetClientManager().GetClientByUserId(item.UserId);
        var clientHabbo = client?.GetHabbo();
        var furniture = clientHabbo?.Inventory?.Furniture;
        if (client != null && furniture != null)
        {
            furniture.AddItem(item.ToInventoryItem());
            client.Send(new FurniListUpdateComposer());
        }

        return true;
    }

    private void NormalizeWallItemPosition(Item item)
    {
        if (string.IsNullOrWhiteSpace(item.WallCoordinates))
        {
            PersistDefaultWallPosition(item);
            item.WallCoordinates = DefaultWallPosition;
            return;
        }

        try
        {
            var wallParts = item.WallCoordinates.Split(':');
            if (wallParts.Length < 2)
                throw new FormatException("Invalid wall position");

            item.WallCoordinates = WallPositionCheck($":{wallParts[1]}") ?? DefaultWallPosition;
        }
        catch
        {
            PersistDefaultWallPosition(item);
            item.WallCoordinates = DefaultWallPosition;
        }
    }

    private void PersistDefaultWallPosition(Item item)
    {
        using var connection = _room.GetDatabase().Connection();
        connection.Execute(
            "UPDATE `items` SET `wall_pos` = @wallPosition WHERE `id` = @id LIMIT 1",
            new { wallPosition = DefaultWallPosition, id = item.Id });
    }

    private void RegisterLoadedItem(Item item)
    {
        if (item.IsFloorItem)
        {
            if (!_floorItems.ContainsKey(item.Id))
                _floorItems.TryAdd(item.Id, item);
            return;
        }

        if (item.IsWallItem && !_wallItems.ContainsKey(item.Id))
            _wallItems.TryAdd(item.Id, item);
    }

    private void InitializeLoadedFloorItemState()
    {
        foreach (var item in _floorItems.Values.ToList())
            InitializeLoadedFloorItem(item);
    }

    private void InitializeLoadedFloorItem(Item item)
    {
        if (item.IsRoller)
        {
            GotRollers = true;
            return;
        }

        if (item.Definition.InteractionType == InteractionType.Moodlight)
        {
            if (_room.MoodlightData == null)
                _room.MoodlightData = new(item.Id, _room.GetDatabase());
            return;
        }

        if (item.Definition.InteractionType == InteractionType.Toner)
        {
            if (_room.TonerData == null)
                _room.TonerData = new(item.Id, _room.GetDatabase());
            return;
        }

        if (item.IsWired)
        {
            if (_room.GetWired() == null)
                return;

            _room.GetWired().LoadWiredBox(item);
            return;
        }

        if (item.Definition.InteractionType == InteractionType.Hopper)
            HopperCount++;
    }

    public Item GetItem(uint itemId)
    {
        if (TryGetLoadedItem(itemId, out var item))
            return item;

        return null!;
    }

    private bool TryGetLoadedItem(uint itemId, out Item item)
    {
        if (_floorItems.TryGetValue(itemId, out item!))
            return true;

        if (_wallItems.TryGetValue(itemId, out item!))
            return true;

        item = null!;
        return false;
    }

    public void RemoveFurniture(GameClient? session, uint itemId)
    {
        var item = GetItem(itemId);
        if (item == null)
            return;

        PrepareItemRemoval(session, item);
        RemoveRoomItem(item);
    }

    private void PrepareItemRemoval(GameClient? session, Item item)
    {
        UnregisterSpecialRemovalTargets(item);
        RunItemRemovalInteractor(session, item);
        ResetGuildGateUpdateState(item);
    }

    private void UnregisterSpecialRemovalTargets(Item item)
    {
        if (item.Definition.InteractionType == InteractionType.FootballGate)
            _room.GetSoccer().UnRegisterGate(item);
    }

    private static void RunItemRemovalInteractor(GameClient? session, Item item)
    {
        if (item.Definition.InteractionType != InteractionType.Gift)
            item.Interactor.OnRemove(session!, item);
    }

    private static void ResetGuildGateUpdateState(Item item)
    {
        if (item.Definition.InteractionType != InteractionType.GuildGate)
            return;

        item.UpdateCounter = 0;
        item.UpdateNeeded = false;
    }

    private void RemoveRoomItem(Item item)
    {
        BroadcastItemRemoval(item);
        RemoveLoadedItem(item);
        RemoveItem(item);
        _room.GetGameMap().GenerateMaps();
        _room.GetRoomUserManager().UpdateUserStatusses();
    }

    private void BroadcastItemRemoval(Item item)
    {
        if (item.IsFloorItem)
            _room.SendPacket(new ObjectRemoveComposer(item, item.UserId));
        else if (item.IsWallItem)
            _room.SendPacket(new ItemRemoveComposer(item, item.UserId));
    }

    private void RemoveLoadedItem(Item item)
    {
        //TODO: Recode this specific part
        if (item.IsWallItem)
        {
            _wallItems.TryRemove(item.Id, out _);
            return;
        }

        _floorItems.TryRemove(item.Id, out var removedItem);
        if (removedItem != null)
            _room.GetGameMap().RemoveFromMap(removedItem);
    }

    private List<IServerPacket> CycleRollers()
    {
        if (!GotRollers)
            return new();

        if (_mRollerCycle >= _mRollerSpeed || _mRollerSpeed == 0)
        {
            ResetRollerCycleState();
            foreach (var roller in _rollers.Values.ToList())
            {
                if (roller == null)
                    continue;

                ProcessRoller(roller);
            }

            _mRollerCycle = 0;
            return _rollerMessages;
        }

        _mRollerCycle++;
        return new();
    }

    private void ResetRollerCycleState()
    {
        _rollerItemsMoved.Clear();
        _rollerUsersMoved.Clear();
        _rollerMessages.Clear();
    }

    private void ProcessRoller(Item roller)
    {
        var nextSquare = roller.SquareInFront;
        var itemsOnRoller = GetItemsOnRoller(roller);
        var itemsOnNext = _room.GetGameMap().GetAllRoomItemForSquare(nextSquare.X, nextSquare.Y).ToList();
        var targetState = GetRollerTargetState(itemsOnNext);

        MoveRollerItems(roller, nextSquare, itemsOnRoller, targetState);
        MoveRollerUser(roller, nextSquare, targetState);
    }

    private List<Item> GetItemsOnRoller(Item roller)
    {
        var itemsOnRoller = _room.GetGameMap().GetRoomItemForSquare(roller.GetX, roller.GetY, roller.GetZ);
        if (itemsOnRoller.Count > 10)
            return itemsOnRoller.Take(10).ToList();

        return itemsOnRoller;
    }

    private static RollerTargetState GetRollerTargetState(List<Item> itemsOnNext)
    {
        var nextRollerHeight = 0.0;
        var nextSquareIsRoller = false;

        foreach (var item in itemsOnNext.ToList())
        {
            if (!item.IsRoller)
                continue;

            if (item.TotalHeight > nextRollerHeight)
                nextRollerHeight = item.TotalHeight;

            nextSquareIsRoller = true;
        }

        var nextRollerClear = true;
        if (nextSquareIsRoller)
        {
            foreach (var item in itemsOnNext.ToList())
            {
                if (item.TotalHeight > nextRollerHeight)
                    nextRollerClear = false;
            }
        }

        return new()
        {
            NextSquareIsRoller = nextSquareIsRoller,
            NextRollerClear = nextRollerClear,
            NextRollerHeight = nextRollerHeight
        };
    }

    private void MoveRollerItems(Item roller, Point nextSquare, List<Item> itemsOnRoller, RollerTargetState targetState)
    {
        if (itemsOnRoller.Count == 0)
            return;

        foreach (var rollerItem in itemsOnRoller.ToList())
        {
            if (!CanMoveRollerItem(roller, rollerItem, nextSquare, targetState))
                continue;

            var nextZ = targetState.NextSquareIsRoller ? rollerItem.GetZ : rollerItem.GetZ - roller.Definition.Height;
            _rollerMessages.Add(UpdateItemOnRoller(rollerItem, nextSquare, roller.Id, nextZ));
            _rollerItemsMoved.Add(rollerItem.Id);
        }
    }

    private bool CanMoveRollerItem(Item roller, Item? rollerItem, Point nextSquare, RollerTargetState targetState)
    {
        if (rollerItem == null)
            return false;

        return !_rollerItemsMoved.Contains(rollerItem.Id) &&
               _room.GetGameMap().CanRollItemHere(nextSquare.X, nextSquare.Y) &&
               targetState.NextRollerClear &&
               roller.GetZ < rollerItem.GetZ &&
               _room.GetRoomUserManager().GetUserForSquare(nextSquare.X, nextSquare.Y) == null;
    }

    private void MoveRollerUser(Item roller, Point nextSquare, RollerTargetState targetState)
    {
        var rollerUser = _room.GetGameMap().GetRoomUsers(roller.Coordinate).FirstOrDefault();
        if (!CanMoveRollerUser(roller, rollerUser, nextSquare, targetState))
            return;

        var nextZ = targetState.NextSquareIsRoller ? rollerUser!.Z : rollerUser!.Z - roller.Definition.Height;
        rollerUser.IsRolling = true;
        rollerUser.RollerDelay = 1;
        _rollerMessages.Add(UpdateUserOnRoller(rollerUser, nextSquare, roller.Id, nextZ));
        _rollerUsersMoved.Add(rollerUser.HabboId);
    }

    private bool CanMoveRollerUser(Item roller, RoomUser? rollerUser, Point nextSquare, RollerTargetState targetState)
    {
        if (rollerUser == null || rollerUser.IsWalking || _rollerUsersMoved.Contains(rollerUser.HabboId))
            return false;

        return targetState.NextRollerClear &&
               _room.GetGameMap().IsValidStep(new(roller.GetX, roller.GetY), new(nextSquare.X, nextSquare.Y), true, false, true) &&
               _room.GetGameMap().CanRollItemHere(nextSquare.X, nextSquare.Y) &&
               _room.GetGameMap().GetFloorStatus(nextSquare) != 0;
    }

    public IServerPacket UpdateItemOnRoller(Item pItem, Point nextCoord, uint pRolledId, double nextZ)
    {
        var mMessage = new SlideObjectBundleComposer(pItem.GetX, pItem.GetY, pItem.GetZ, nextCoord.X, nextCoord.Y, nextZ, pRolledId, 0, pItem.Id);
        SetFloorItem(pItem, nextCoord.X, nextCoord.Y, nextZ);
        return mMessage;
    }

    public IServerPacket UpdateUserOnRoller(RoomUser pUser, Point pNextCoord, uint pRollerId, double nextZ)
    {
        var mMessage = new SlideObjectBundleComposer(pUser.X, pUser.Y, pUser.Z, pNextCoord.X, pNextCoord.Y, nextZ, pRollerId, pUser.VirtualId, 0);
        _room.GetGameMap().UpdateUserMovement(new(pUser.X, pUser.Y), new(pNextCoord.X, pNextCoord.Y), pUser);
        _room.GetGameMap().GameMap[pUser.X, pUser.Y] = 1;
        pUser.X = pNextCoord.X;
        pUser.Y = pNextCoord.Y;
        pUser.Z = nextZ;
        _room.GetGameMap().GameMap[pUser.X, pUser.Y] = 0;
        var client = pUser?.GetClient();
        var habbo = client?.GetHabbo();
        TriggerRollerUserWiredEvents(habbo, pNextCoord, pRollerId);
        return mMessage;
    }

    private void TriggerRollerUserWiredEvents(Habbo? habbo, Point nextCoord, uint rollerId)
    {
        if (habbo == null)
            return;

        TriggerWalkOnEvents(habbo, nextCoord);
        TriggerWalkOffRollerEvent(habbo, rollerId);
    }

    private void TriggerWalkOnEvents(Habbo habbo, Point nextCoord)
    {
        var items = _room.GetGameMap().GetRoomItemForSquare(nextCoord.X, nextCoord.Y);
        foreach (var item in items.ToList())
        {
            if (item == null)
                continue;

            _room.GetWired().TriggerEvent(WiredBoxType.TriggerWalkOnFurni, habbo, item);
        }
    }

    private void TriggerWalkOffRollerEvent(Habbo habbo, uint rollerId)
    {
        var roller = _room.GetRoomItemHandler().GetItem(rollerId);
        if (roller != null)
            _room.GetWired().TriggerEvent(WiredBoxType.TriggerWalkOffFurni, habbo, roller);
    }

    private void SaveFurniture()
    {
        try
        {
            if (_movedItems.Count == 0)
                return;

            using var connection = _room.GetDatabase().Connection();
            foreach (var item in _movedItems.Values.ToList())
                PersistMovedItem(connection, item);
        }
        catch (Exception e)
        {
            ExceptionLogger.LogCriticalException(e);
        }
    }

    private void PersistMovedItem(System.Data.IDbConnection connection, Item item)
    {
        PersistMovedItemExtraData(connection, item);
        PersistMovedWallItemPosition(connection, item);
        PersistMovedItemCoordinates(connection, item);
    }

    private static void PersistMovedItemExtraData(System.Data.IDbConnection connection, Item item)
    {
        if (string.IsNullOrEmpty(item.LegacyDataString))
            return;

        connection.Execute(
            "UPDATE `items` SET `extra_data` = @extraData WHERE `id` = @id LIMIT 1",
            new { extraData = item.ExtraData.Serialize(), id = item.Id });
    }

    private static void PersistMovedWallItemPosition(System.Data.IDbConnection connection, Item item)
    {
        if (!item.IsWallItem || IsRoomSurfaceDecoration(item))
            return;

        connection.Execute(
            "UPDATE `items` SET `wall_pos` = @wallPos WHERE `id` = @id LIMIT 1",
            new { wallPos = item.WallCoordinates, id = item.Id });
    }

    private static bool IsRoomSurfaceDecoration(Item item) =>
        item.Definition.ItemName.Contains("wallpaper_single") ||
        item.Definition.ItemName.Contains("floor_single") ||
        item.Definition.ItemName.Contains("landscape_single");

    private static void PersistMovedItemCoordinates(System.Data.IDbConnection connection, Item item)
    {
        connection.Execute(
            "UPDATE `items` SET `x` = @x, `y` = @y, `z` = @z, `rot` = @rot WHERE `id` = @id LIMIT 1",
            new { x = item.GetX, y = item.GetY, z = item.GetZ, rot = item.Rotation, id = item.Id });
    }

    public bool SetFloorItem(GameClient session, Item item, int newX, int newY, int newRot, bool newItem, bool onRoller, bool sendMessage, bool updateRoomUserStatuses = false, double height = -1)
    {
        var needsReAdd = false;
        if (!CanPlaceNewFloorItem(item, newItem))
            return false;

        var itemsOnTile = GetFurniObjects(newX, newY);
        if (HasConflictingRoller(item, itemsOnTile))
            return false;

        if (!newItem)
            needsReAdd = _room.GetGameMap().RemoveFromMap(item);

        var affectedTiles = Gamemap.GetAffectedTiles(item.Definition.Length, item.Definition.Width, newX, newY, newRot);
        if (!ValidateFloorPlacement(item, newX, newY, onRoller, needsReAdd, affectedTiles))
            return false;

        if (!TryResolveFloorPlacement(item, newX, newY, newRot, onRoller, needsReAdd, height, affectedTiles, itemsOnTile, out var resolvedRotation, out var resolvedZ))
            return false;

        return ApplyFloorPlacement(session, item, newX, newY, resolvedRotation, resolvedZ, newItem, onRoller, sendMessage, updateRoomUserStatuses, affectedTiles);
    }

    private bool CanPlaceNewFloorItem(Item item, bool newItem)
    {
        if (!newItem || !item.IsWired)
            return true;

        return item.Definition.WiredType != WiredBoxType.EffectRegenerateMaps ||
               _room.GetRoomItemHandler().GetFloor.Count(x => x.Definition.WiredType == WiredBoxType.EffectRegenerateMaps) == 0;
    }

    private static bool HasConflictingRoller(Item item, List<Item> itemsOnTile) =>
        item.Definition.InteractionType == InteractionType.Roller &&
        itemsOnTile.Count(x => x.Definition.InteractionType == InteractionType.Roller && x.Id != item.Id) > 0;

    private bool ValidateFloorPlacement(Item item, int newX, int newY, bool onRoller, bool needsReAdd, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        if (!HasValidTargetTiles(item, newX, newY, affectedTiles) ||
            (!onRoller && !HasOpenPlacementTiles(item, affectedTiles)) ||
            (!onRoller && !HasNoUserBlocking(item, affectedTiles)))
        {
            ReAddItemToMapIfNeeded(item, needsReAdd);
            return false;
        }

        return true;
    }

    private bool HasValidTargetTiles(Item item, int newX, int newY, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        if (!_room.GetGameMap().ValidTile(newX, newY) || (_room.GetGameMap().SquareHasUsers(newX, newY) && !item.Definition.IsSeat))
            return false;

        foreach (var tile in affectedTiles.Values)
        {
            if (!_room.GetGameMap().ValidTile(tile.X, tile.Y) ||
                (_room.GetGameMap().SquareHasUsers(tile.X, tile.Y) && !item.Definition.IsSeat))
                return false;
        }

        return true;
    }

    private bool HasOpenPlacementTiles(Item item, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        foreach (var tile in affectedTiles.Values)
        {
            if (_room.GetGameMap().Model.SqState[tile.X, tile.Y] != SquareState.Open && !item.Definition.IsSeat)
                return false;
        }

        return true;
    }

    private bool HasNoUserBlocking(Item item, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        if (item.Definition.IsSeat || item.IsRoller)
            return true;

        foreach (var tile in affectedTiles.Values)
        {
            if (_room.GetGameMap().GetRoomUsers(new(tile.X, tile.Y)).Count > 0)
                return false;
        }

        return true;
    }

    private void ReAddItemToMapIfNeeded(Item item, bool needsReAdd)
    {
        if (needsReAdd)
            _room.GetGameMap().AddToMap(item);
    }

    private bool TryResolveFloorPlacement(Item item, int newX, int newY, int newRot, bool onRoller, bool needsReAdd, double height, Dictionary<int, ThreeDCoord> affectedTiles, List<Item> itemsOnTile, out int resolvedRotation, out double resolvedZ)
    {
        resolvedRotation = NormalizeFloorItemRotation(item, newRot);
        resolvedZ = height != -1 ? height : _room.GetGameMap().Model.SqFloorHeight[newX, newY];
        if (height != -1)
            return true;

        var itemsComplete = GetAffectedPlacementItems(newX, newY, affectedTiles, itemsOnTile);
        if (!onRoller && !AreStackedItemsPlaceable(item, itemsComplete))
        {
            ReAddItemToMapIfNeeded(item, needsReAdd);
            return false;
        }

        resolvedZ = ResolveFloorPlacementHeight(item, newX, newY, resolvedRotation, resolvedZ, itemsComplete);
        return true;
    }

    private List<Item> GetAffectedPlacementItems(int newX, int newY, Dictionary<int, ThreeDCoord> affectedTiles, List<Item> itemsOnTile)
    {
        var itemsAffected = new List<Item>();
        foreach (var tile in affectedTiles.Values.ToList())
        {
            var temp = GetFurniObjects(tile.X, tile.Y);
            if (temp != null)
                itemsAffected.AddRange(temp);
        }

        var itemsComplete = new List<Item>();
        itemsComplete.AddRange(itemsOnTile);
        itemsComplete.AddRange(itemsAffected);
        return itemsComplete;
    }

    private bool AreStackedItemsPlaceable(Item item, List<Item> itemsComplete)
    {
        foreach (var stackedItem in itemsComplete.ToList())
        {
            if (stackedItem == null || stackedItem.Id == item.Id || stackedItem.Definition == null)
                continue;

            if (!stackedItem.Definition.Stackable)
                return false;
        }

        return true;
    }

    private static int NormalizeFloorItemRotation(Item item, int newRot)
    {
        if (newRot != 0 && newRot != 2 && newRot != 4 && newRot != 6 && newRot != 8 && !item.Definition.ExtraRot)
            return 0;

        return newRot;
    }

    private double ResolveFloorPlacementHeight(Item item, int newX, int newY, int newRot, double baseZ, List<Item> itemsComplete)
    {
        var resolvedZ = baseZ;
        if (item.Rotation != newRot && item.GetX == newX && item.GetY == newY)
            resolvedZ = item.GetZ;

        foreach (var stackedItem in itemsComplete.ToList())
        {
            if (stackedItem == null || stackedItem.Id == item.Id)
                continue;

            if (stackedItem.Definition.InteractionType == InteractionType.Stacktool)
            {
                resolvedZ = stackedItem.GetZ;
                break;
            }

            if (stackedItem.TotalHeight > resolvedZ)
                resolvedZ = stackedItem.TotalHeight;
        }

        return resolvedZ;
    }

    private bool ApplyFloorPlacement(GameClient session, Item item, int newX, int newY, int newRot, double newZ, bool newItem, bool onRoller, bool sendMessage, bool updateRoomUserStatuses, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        item.Rotation = newRot;
        item.SetState(newX, newY, newZ, affectedTiles);
        if (!onRoller && session != null)
            item.Interactor.OnPlace(session, item);

        if (newItem)
        {
            if (_floorItems.ContainsKey(item.Id))
            {
                if (session != null)
                    session.SendNotification(PlusEnvironment.LanguageManager.TryGetValue("room.item.already_placed"));
                _room.GetGameMap().RemoveFromMap(item);
                return true;
            }

            if (item.IsFloorItem && !_floorItems.ContainsKey(item.Id))
                _floorItems.TryAdd(item.Id, item);
            else if (item.IsWallItem && !_wallItems.ContainsKey(item.Id))
                _wallItems.TryAdd(item.Id, item);

            if (sendMessage)
                _room.SendPacket(new ObjectAddComposer(item));
        }
        else
        {
            UpdateItem(item);
            if (!onRoller && sendMessage)
                _room.SendPacket(new ObjectUpdateComposer(item));
        }

        _room.GetGameMap().AddToMap(item);
        if (item.Definition.IsSeat)
            updateRoomUserStatuses = true;
        if (updateRoomUserStatuses)
            _room.GetRoomUserManager().UpdateUserStatusses();
        if (item.Definition.InteractionType == InteractionType.Tent || item.Definition.InteractionType == InteractionType.TentSmall)
        {
            _room.RemoveTent(item.Id);
            _room.AddTent(item.Id);
        }

        using var connection = _room.GetDatabase().Connection();
        connection.Execute(
            "UPDATE `items` SET `room_id` = @roomId, `x` = @x, `y` = @y, `z` = @z, `rot` = @rot WHERE `id` = @id LIMIT 1",
            new { roomId = _room.RoomId, x = item.GetX, y = item.GetY, z = item.GetZ, rot = item.Rotation, id = item.Id });
        return true;
    }


    public List<Item> GetFurniObjects(int x, int y) => _room.GetGameMap().GetCoordinatedItems(new(x, y));

    public bool SetFloorItem(Item item, int newX, int newY, double newZ)
    {
        if (_room == null)
            return false;

        _room.GetGameMap().RemoveFromMap(item);
        item.SetState(newX, newY, newZ, Gamemap.GetAffectedTiles(item.Definition.Length, item.Definition.Width, newX, newY, item.Rotation));
        EnsureTonerData(item);
        UpdateItem(item);
        _room.GetGameMap().AddItemToMap(item);
        return true;
    }

    private void EnsureTonerData(Item item)
    {
        if (item.Definition.InteractionType != InteractionType.Toner)
            return;

        if (_room.TonerData == null)
            _room.TonerData = new(item.Id, _room.GetDatabase());
    }

    public bool SetWallItem(GameClient session, Item item)
    {
        if (!CanPlaceWallItem(item))
            return false;

        if (IsWallItemAlreadyPlacedOnFloor(session, item))
            return true;

        PlaceWallItem(session, item);
        InitializeWallItemState(item);
        PersistWallItemPlacement(item);
        _wallItems.TryAdd(item.Id, item);
        _room.SendPacket(new ItemAddComposer(item));
        return true;
    }

    private bool CanPlaceWallItem(Item item) =>
        item.IsWallItem && !_wallItems.ContainsKey(item.Id);

    private bool IsWallItemAlreadyPlacedOnFloor(GameClient session, Item item)
    {
        if (!_floorItems.ContainsKey(item.Id))
            return false;

        session.SendNotification(PlusEnvironment.LanguageManager.TryGetValue("room.item.already_placed"));
        return true;
    }

    private static void PlaceWallItem(GameClient session, Item item)
    {
        item.Interactor.OnPlace(session, item);
    }

    private void InitializeWallItemState(Item item)
    {
        if (item.Definition.InteractionType != InteractionType.Moodlight)
            return;

        if (_room.MoodlightData == null)
        {
            _room.MoodlightData = new(item.Id, _room.GetDatabase());
            item.LegacyDataString = _room.MoodlightData.GenerateExtraData();
        }
    }

    private void PersistWallItemPlacement(Item item)
    {
        using var connection = _room.GetDatabase().Connection();
        connection.Execute(
            "UPDATE `items` SET `room_id` = @roomId, `x` = @x, `y` = @y, `z` = @z, `rot` = @rot, `wall_pos` = @wallPos WHERE `id` = @id LIMIT 1",
            new
            {
                roomId = _room.RoomId,
                x = item.GetX,
                y = item.GetY,
                z = item.GetZ,
                rot = item.Rotation,
                wallPos = item.WallCoordinates,
                id = item.Id
            });
    }

    public void UpdateItem(Item item)
    {
        if (item == null)
            return;

        TrackMovedItem(item);
    }

    private void TrackMovedItem(Item item)
    {
        if (!_movedItems.ContainsKey(item.Id))
            _movedItems.TryAdd(item.Id, item);
    }

    public void RemoveItem(Item item)
    {
        if (item == null)
            return;

        UntrackMovedItem(item);
        UntrackRoller(item);
    }

    private void UntrackMovedItem(Item item)
    {
        if (_movedItems.ContainsKey(item.Id))
            _movedItems.TryRemove(item.Id, out _);
    }

    private void UntrackRoller(Item item)
    {
        if (_rollers.ContainsKey(item.Id))
            _rollers.TryRemove(item.Id, out _);
    }

    public void OnCycle()
    {
        RunRollerCycle();
        ProcessQueuedItemUpdates();
    }

    private void RunRollerCycle()
    {
        if (!GotRollers)
            return;

        try
        {
            _room.SendPacket(CycleRollers());
        }
        catch //(Exception e)
        {
            // Logging.LogThreadException(e.ToString(), "rollers for room with ID " + room.RoomId);
            GotRollers = false;
        }
    }

    private void ProcessQueuedItemUpdates()
    {
        if (_roomItemUpdateQueue.Count == 0)
            return;

        var pendingItems = DequeueItemsNeedingFurtherUpdates();
        RequeuePendingItems(pendingItems);
    }

    private List<Item> DequeueItemsNeedingFurtherUpdates()
    {
        var pendingItems = new List<Item>();
        while (_roomItemUpdateQueue.Count > 0)
        {
            if (!_roomItemUpdateQueue.TryDequeue(out var item) || item == null)
                continue;

            item.ProcessUpdates();
            if (item.UpdateCounter > 0)
                pendingItems.Add(item);
        }

        return pendingItems;
    }

    private void RequeuePendingItems(List<Item> pendingItems)
    {
        foreach (var item in pendingItems.ToList())
        {
            if (item == null)
                continue;

            _roomItemUpdateQueue.Enqueue(item);
        }
    }

    public List<Item> RemoveItems(GameClient session)
    {
        var items = new List<Item>();
        var habbo = session.GetHabbo();
        var inventory = habbo?.Inventory?.Furniture;
        if (habbo == null || inventory == null)
            return items;

        foreach (var item in GetWallAndFloor.ToList())
        {
            if (!CanRemoveOwnedItem(item, habbo.Id))
                continue;

            RemoveOwnedItem(item, inventory);
            session.Send(new FurniListAddComposer(item.ToInventoryItem()));
        }

        _rollers.Clear();
        return items;
    }

    private static bool CanRemoveOwnedItem(Item? item, int ownerId) =>
        item != null && item.UserId == ownerId;

    private void RemoveOwnedItem(Item item, FurnitureInventoryComponent inventory)
    {
        if (item.IsFloorItem)
        {
            RemoveOwnedFloorItem(item, inventory);
            return;
        }

        if (item.IsWallItem)
            RemoveOwnedWallItem(item, inventory);
    }

    private void RemoveOwnedFloorItem(Item item, FurnitureInventoryComponent inventory)
    {
        _floorItems.TryRemove(item.Id, out var removedItem);
        AddRemovedItemToInventory(removedItem, inventory);
        _room.SendPacket(new ObjectRemoveComposer(item, item.UserId));
    }

    private void RemoveOwnedWallItem(Item item, FurnitureInventoryComponent inventory)
    {
        _wallItems.TryRemove(item.Id, out var removedItem);
        AddRemovedItemToInventory(removedItem, inventory);
        _room.SendPacket(new ItemRemoveComposer(item, item.UserId));
    }

    private static void AddRemovedItemToInventory(Item? removedItem, FurnitureInventoryComponent inventory)
    {
        // TODO @80O: Items refactor
        if (removedItem != null)
            inventory.AddItem(removedItem.ToInventoryItem());
    }


    public bool CheckPosItem(Item item, int newX, int newY, int newRot)
    {
        try
        {
            var affectedTiles = Gamemap.GetAffectedTiles(item.Definition.Length, item.Definition.Width, newX, newY, newRot);
            if (!HasValidCheckPositionTiles(newX, newY, affectedTiles))
                return false;
            if (IntersectsDoorTile(newX, newY, affectedTiles))
                return false;
            if (!HasMatchingBaseHeight(item, newX, newY, newRot))
                return false;
            if (!HasOpenCheckPositionTiles(newX, newY, affectedTiles))
                return false;
            if (!HasNoBlockingUsers(item, newX, newY, affectedTiles))
                return false;

            return HasOnlyStackableItems(item, newX, newY, affectedTiles);
        }
        catch
        {
            return false;
        }
    }

    private bool HasValidCheckPositionTiles(int newX, int newY, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        if (!_room.GetGameMap().ValidTile(newX, newY))
            return false;

        foreach (var coord in affectedTiles.Values)
        {
            if (!_room.GetGameMap().ValidTile(coord.X, coord.Y))
                return false;
        }

        return true;
    }

    private bool IntersectsDoorTile(int newX, int newY, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        if (_room.GetGameMap().Model.DoorX == newX && _room.GetGameMap().Model.DoorY == newY)
            return true;

        foreach (var coord in affectedTiles.Values)
        {
            if (_room.GetGameMap().Model.DoorX == coord.X && _room.GetGameMap().Model.DoorY == coord.Y)
                return true;
        }

        return false;
    }

    private bool HasMatchingBaseHeight(Item item, int newX, int newY, int newRot)
    {
        var floorHeight = _room.GetGameMap().Model.SqFloorHeight[newX, newY];
        return item.Rotation != newRot || item.GetX != newX || item.GetY != newY || item.GetZ == floorHeight;
    }

    private bool HasOpenCheckPositionTiles(int newX, int newY, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        if (_room.GetGameMap().Model.SqState[newX, newY] != SquareState.Open)
            return false;

        foreach (var coord in affectedTiles.Values)
        {
            if (_room.GetGameMap().Model.SqState[coord.X, coord.Y] != SquareState.Open)
                return false;
        }

        return true;
    }

    private bool HasNoBlockingUsers(Item item, int newX, int newY, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        if (item.Definition.IsSeat)
            return true;

        if (_room.GetGameMap().SquareHasUsers(newX, newY))
            return false;

        foreach (var coord in affectedTiles.Values)
        {
            if (_room.GetGameMap().SquareHasUsers(coord.X, coord.Y))
                return false;
        }

        return true;
    }

    private bool HasOnlyStackableItems(Item item, int newX, int newY, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        var itemsOnTarget = new List<Item>();
        itemsOnTarget.AddRange(GetFurniObjects(newX, newY));

        foreach (var coord in affectedTiles.Values)
        {
            var coordinatedItems = GetFurniObjects(coord.X, coord.Y);
            if (coordinatedItems != null)
                itemsOnTarget.AddRange(coordinatedItems);
        }

        foreach (var roomItem in itemsOnTarget)
        {
            if (roomItem.Id != item.Id && !roomItem.Definition.Stackable)
                return false;
        }

        return true;
    }


    public ICollection<Item> GetRollers() => _rollers.Values;

    public void Dispose()
    {
        SaveFurniture();
        DestroyLoadedItems();
        ClearTrackedState();
        _roomItemUpdateQueue = null!;
    }

    private void DestroyLoadedItems()
    {
        foreach (var item in GetWallAndFloor.ToList())
        {
            if (item == null)
                continue;

            item.Destroy();
        }
    }

    private void ClearTrackedState()
    {
        _movedItems.Clear();
        _rollers.Clear();
        _wallItems.Clear();
        _floorItems.Clear();
        _rollerItemsMoved.Clear();
        _rollerUsersMoved.Clear();
        _rollerMessages.Clear();
    }
}
