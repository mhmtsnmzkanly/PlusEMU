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

    private readonly ConcurrentDictionary<uint, Item> _floorItems;
    private readonly ConcurrentDictionary<uint, Item> _movedItems;
    private readonly List<uint> _rollerItemsMoved;
    private readonly List<IServerPacket> _rollerMessages;

    private readonly ConcurrentDictionary<uint, Item> _rollers;
    private readonly List<int> _rollerUsersMoved;
    private readonly Room _room;
    private readonly ConcurrentDictionary<uint, Item> _wallItems;
    private readonly IItemLoader _itemLoader;
    private readonly IRoomItemPersistenceService _roomItemPersistenceService;
    private readonly IRoomItemPlacementValidatorService _roomItemPlacementValidatorService;
    private readonly IRoomItemPlacementPersistenceService _roomItemPlacementPersistenceService;
    private readonly IRoomRollerService _roomRollerService;
    private readonly IRoomItemInventoryService _roomItemInventoryService;
    private readonly IRoomItemUpdateQueueService _roomItemUpdateQueueService;
    private readonly IRoomItemLoadService _roomItemLoadService;
    private readonly IRoomItemRemovalService _roomItemRemovalService;
    private readonly IRoomItemStateService _roomItemStateService;
    private readonly IRoomItemPlacementApplyService _roomItemPlacementApplyService;
    private readonly IRoomItemTrackingService _roomItemTrackingService;
    private int _mRollerCycle;
    private int _mRollerSpeed;

    private ConcurrentQueue<Item> _roomItemUpdateQueue;

    public int HopperCount;
    public bool GotRollers { get; set; }

    public RoomItemHandling(Room room, IItemLoader itemLoader, IRoomItemPersistenceService roomItemPersistenceService, IRoomItemPlacementValidatorService roomItemPlacementValidatorService, IRoomItemPlacementPersistenceService roomItemPlacementPersistenceService, IRoomRollerService roomRollerService, IRoomItemInventoryService roomItemInventoryService, IRoomItemUpdateQueueService roomItemUpdateQueueService, IRoomItemLoadService roomItemLoadService, IRoomItemRemovalService roomItemRemovalService, IRoomItemStateService roomItemStateService, IRoomItemPlacementApplyService roomItemPlacementApplyService, IRoomItemTrackingService roomItemTrackingService)
    {
        _room = room;
        _itemLoader = itemLoader;
        _roomItemPersistenceService = roomItemPersistenceService;
        _roomItemPlacementValidatorService = roomItemPlacementValidatorService;
        _roomItemPlacementPersistenceService = roomItemPlacementPersistenceService;
        _roomRollerService = roomRollerService;
        _roomItemInventoryService = roomItemInventoryService;
        _roomItemUpdateQueueService = roomItemUpdateQueueService;
        _roomItemLoadService = roomItemLoadService;
        _roomItemRemovalService = roomItemRemovalService;
        _roomItemStateService = roomItemStateService;
        _roomItemPlacementApplyService = roomItemPlacementApplyService;
        _roomItemTrackingService = roomItemTrackingService;
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
        _roomItemLoadService.ResetLoadedFurnitureState(_floorItems.Values, _wallItems.Values);
        var items = _itemLoader.GetItemsForRoom(_room.Id, _room);
        foreach (var item in items.ToList())
        {
            if (item == null)
                continue;

            _roomItemLoadService.EnsureOwnedItemUser(_room, item);

            if (item.IsFloorItem)
            {
                if (_roomItemLoadService.TryRecoverInvalidFloorItem(_room, item))
                    continue;

                _roomItemTrackingService.RegisterLoadedItem(_floorItems, _wallItems, item);
            }
            else if (item.IsWallItem)
            {
                _roomItemLoadService.NormalizeWallItemPosition(_room, item, DefaultWallPosition, WallPositionCheck);
                _roomItemTrackingService.RegisterLoadedItem(_floorItems, _wallItems, item);
            }
        }
        InitializeLoadedFloorItemState();
    }

    private void InitializeLoadedFloorItemState()
    {
        foreach (var item in _floorItems.Values.ToList())
            InitializeLoadedFloorItem(item);
    }

    private void InitializeLoadedFloorItem(Item item)
    {
        var result = _roomItemStateService.InitializeLoadedFloorItem(_room, item);
        if (result.HasRoller)
            GotRollers = true;
        if (result.HopperDelta != 0)
            HopperCount += result.HopperDelta;
    }

    public Item GetItem(uint itemId)
    {
        if (_roomItemTrackingService.TryGetLoadedItem(_floorItems, _wallItems, itemId, out var item))
            return item;

        return null!;
    }

    public void RemoveFurniture(GameClient? session, uint itemId)
    {
        var item = GetItem(itemId);
        if (item == null)
            return;

        _roomItemRemovalService.PrepareItemRemoval(_room, session, item);
        RemoveRoomItem(item);
    }

    private void RemoveRoomItem(Item item)
    {
        _roomItemRemovalService.BroadcastItemRemoval(_room, item);
        _roomItemTrackingService.RemoveLoadedItem(_room, _floorItems, _wallItems, item);
        RemoveItem(item);
        _room.GetGameMap().GenerateMaps();
        _room.GetRoomUserManager().UpdateUserStatusses();
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
        var itemsOnRoller = _roomRollerService.GetItemsOnRoller(_room, roller);
        var itemsOnNext = _room.GetGameMap().GetAllRoomItemForSquare(nextSquare.X, nextSquare.Y).ToList();
        var targetState = _roomRollerService.GetTargetState(itemsOnNext);

        MoveRollerItems(roller, nextSquare, itemsOnRoller, targetState);
        MoveRollerUser(roller, nextSquare, targetState);
    }

    private void MoveRollerItems(Item roller, Point nextSquare, List<Item> itemsOnRoller, RoomRollerTargetState targetState)
    {
        if (itemsOnRoller.Count == 0)
            return;

        foreach (var rollerItem in itemsOnRoller.ToList())
        {
            if (!_roomRollerService.CanMoveItem(_room, roller, rollerItem, nextSquare, targetState, _rollerItemsMoved))
                continue;

            var nextZ = targetState.NextSquareIsRoller ? rollerItem.GetZ : rollerItem.GetZ - roller.Definition.Height;
            _rollerMessages.Add(UpdateItemOnRoller(rollerItem, nextSquare, roller.Id, nextZ));
            _rollerItemsMoved.Add(rollerItem.Id);
        }
    }

    private void MoveRollerUser(Item roller, Point nextSquare, RoomRollerTargetState targetState)
    {
        var rollerUser = _room.GetGameMap().GetRoomUsers(roller.Coordinate).FirstOrDefault();
        if (!_roomRollerService.CanMoveUser(_room, roller, rollerUser, nextSquare, targetState, _rollerUsersMoved))
            return;

        var nextZ = targetState.NextSquareIsRoller ? rollerUser!.Z : rollerUser!.Z - roller.Definition.Height;
        rollerUser.IsRolling = true;
        rollerUser.RollerDelay = 1;
        _rollerMessages.Add(UpdateUserOnRoller(rollerUser, nextSquare, roller.Id, nextZ));
        _rollerUsersMoved.Add(rollerUser.HabboId);
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

            _roomItemPersistenceService.SaveMovedItems(_movedItems.Values.ToList());
        }
        catch (Exception e)
        {
            ExceptionLogger.LogCriticalException(e);
        }
    }

    public bool SetFloorItem(GameClient session, Item item, int newX, int newY, int newRot, bool newItem, bool onRoller, bool sendMessage, bool updateRoomUserStatuses = false, double height = -1)
    {
        var needsReAdd = false;
        if (!_roomItemPlacementValidatorService.CanPlaceNewFloorItem(_room, item, newItem))
            return false;

        var itemsOnTile = GetFurniObjects(newX, newY);
        if (_roomItemPlacementValidatorService.HasConflictingRoller(item, itemsOnTile))
            return false;

        if (!newItem)
            needsReAdd = _room.GetGameMap().RemoveFromMap(item);

        var affectedTiles = Gamemap.GetAffectedTiles(item.Definition.Length, item.Definition.Width, newX, newY, newRot);
        if (!_roomItemPlacementValidatorService.ValidateFloorPlacement(_room, item, newX, newY, onRoller, affectedTiles))
        {
            ReAddItemToMapIfNeeded(item, needsReAdd);
            return false;
        }

        if (!_roomItemPlacementValidatorService.TryResolveFloorPlacement(_room, item, newX, newY, newRot, onRoller, height, affectedTiles, itemsOnTile, out var resolvedRotation, out var resolvedZ))
        {
            ReAddItemToMapIfNeeded(item, needsReAdd);
            return false;
        }

        return _roomItemPlacementApplyService.ApplyFloorPlacement(_room, session, item, newX, newY, resolvedRotation, resolvedZ, newItem, onRoller, sendMessage, updateRoomUserStatuses, affectedTiles, _floorItems, _wallItems, UpdateItem);
    }

    private void ReAddItemToMapIfNeeded(Item item, bool needsReAdd)
    {
        if (needsReAdd)
            _room.GetGameMap().AddToMap(item);
    }

    public List<Item> GetFurniObjects(int x, int y) => _room.GetGameMap().GetCoordinatedItems(new(x, y));

    public bool SetFloorItem(Item item, int newX, int newY, double newZ)
    {
        if (_room == null)
            return false;

        return _roomItemPlacementApplyService.ApplyRollerFloorPlacement(_room, item, newX, newY, newZ, UpdateItem);
    }

    public bool SetWallItem(GameClient session, Item item)
    {
        return _roomItemPlacementApplyService.ApplyWallPlacement(_room, session, item, _floorItems, _wallItems);
    }

    public void UpdateItem(Item item)
    {
        if (item == null)
            return;

        _roomItemTrackingService.TrackMovedItem(_movedItems, item);
    }

    public void RemoveItem(Item item)
    {
        if (item == null)
            return;

        _roomItemTrackingService.RemoveTrackedItem(_movedItems, _rollers, item);
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
        _roomItemUpdateQueueService.Process(_roomItemUpdateQueue);
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
            if (!_roomItemInventoryService.CanRemoveOwnedItem(item, habbo.Id))
                continue;

            RemoveOwnedItem(item, inventory);
            session.Send(new FurniListAddComposer(item.ToInventoryItem()));
        }

        _rollers.Clear();
        return items;
    }

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
        _roomItemInventoryService.AddRemovedItemToInventory(removedItem, inventory);
        _room.SendPacket(new ObjectRemoveComposer(item, item.UserId));
    }

    private void RemoveOwnedWallItem(Item item, FurnitureInventoryComponent inventory)
    {
        _wallItems.TryRemove(item.Id, out var removedItem);
        _roomItemInventoryService.AddRemovedItemToInventory(removedItem, inventory);
        _room.SendPacket(new ItemRemoveComposer(item, item.UserId));
    }


    public bool CheckPosItem(Item item, int newX, int newY, int newRot) =>
        _roomItemPlacementValidatorService.CheckPosItem(_room, item, newX, newY, newRot);


    public ICollection<Item> GetRollers() => _rollers.Values;

    public void Dispose()
    {
        SaveFurniture();
        _roomItemTrackingService.DestroyLoadedItems(GetWallAndFloor);
        _roomItemTrackingService.ClearTrackedState(_movedItems, _rollers, _wallItems, _floorItems, _rollerItemsMoved, _rollerUsersMoved, _rollerMessages);
        _roomItemUpdateQueue = null!;
    }
}
