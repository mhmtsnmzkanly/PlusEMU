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
    private int _mRollerCycle;
    private int _mRollerSpeed;

    private ConcurrentQueue<Item> _roomItemUpdateQueue;

    public int HopperCount;
    public bool GotRollers { get; set; }

    public RoomItemHandling(Room room, IItemLoader itemLoader, IRoomItemPersistenceService roomItemPersistenceService, IRoomItemPlacementValidatorService roomItemPlacementValidatorService, IRoomItemPlacementPersistenceService roomItemPlacementPersistenceService, IRoomRollerService roomRollerService, IRoomItemInventoryService roomItemInventoryService, IRoomItemUpdateQueueService roomItemUpdateQueueService, IRoomItemLoadService roomItemLoadService, IRoomItemRemovalService roomItemRemovalService)
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

                RegisterLoadedItem(item);
            }
            else if (item.IsWallItem)
            {
                _roomItemLoadService.NormalizeWallItemPosition(_room, item, DefaultWallPosition, WallPositionCheck);
                RegisterLoadedItem(item);
            }
        }
        InitializeLoadedFloorItemState();
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

        _roomItemRemovalService.PrepareItemRemoval(_room, session, item);
        RemoveRoomItem(item);
    }

    private void RemoveRoomItem(Item item)
    {
        _roomItemRemovalService.BroadcastItemRemoval(_room, item);
        RemoveLoadedItem(item);
        RemoveItem(item);
        _room.GetGameMap().GenerateMaps();
        _room.GetRoomUserManager().UpdateUserStatusses();
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

        return ApplyFloorPlacement(session, item, newX, newY, resolvedRotation, resolvedZ, newItem, onRoller, sendMessage, updateRoomUserStatuses, affectedTiles);
    }

    private void ReAddItemToMapIfNeeded(Item item, bool needsReAdd)
    {
        if (needsReAdd)
            _room.GetGameMap().AddToMap(item);
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

        _roomItemPlacementPersistenceService.SaveFloorPlacement(_room.RoomId, item);
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
        _roomItemPlacementPersistenceService.SaveWallPlacement(_room.RoomId, item);
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
