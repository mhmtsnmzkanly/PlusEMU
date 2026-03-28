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

namespace Plus.HabboHotel.Rooms;

[Obsolete("Everything in here is bad and whoever wrote this must've been high on some crack or something")]
public class RoomItemHandling
{
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

    public void SetSpeed(int p)
    {
        _mRollerSpeed = p;
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
            var widD = posD[0].Substring(3).Split(',');
            var widthX = int.Parse(widD[0]);
            var widthY = int.Parse(widD[1]);
            if (widthX < -1000 || widthY < -1 || widthX > 700 || widthY > 700)
                return null;
            var lenD = posD[1].Substring(2).Split(',');
            var lengthX = int.Parse(lenD[0]);
            var lengthY = int.Parse(lenD[1]);
            if (lengthX < -1 || lengthY < -1000 || lengthX > 700 || lengthY > 700)
                return null;
            return $":w={widthX},{widthY} l={lengthX},{lengthY} {posD[2]}";
        }
        catch
        {
            return null;
        }
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
            item.WallCoordinates = ":w=0,2 l=11,53 l";
            return;
        }

        try
        {
            var wallParts = item.WallCoordinates.Split(':');
            if (wallParts.Length < 2)
                throw new FormatException("Invalid wall position");

            item.WallCoordinates = WallPositionCheck($":{wallParts[1]}") ?? ":w=0,2 l=11,53 l";
        }
        catch
        {
            PersistDefaultWallPosition(item);
            item.WallCoordinates = ":w=0,2 l=11,53 l";
        }
    }

    private void PersistDefaultWallPosition(Item item)
    {
        using var connection = _room.GetDatabase().Connection();
        connection.Execute(
            "UPDATE `items` SET `wall_pos` = @wallPosition WHERE `id` = @id LIMIT 1",
            new { wallPosition = ":w=0,2 l=11,53 l", id = item.Id });
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
        {
            if (item.IsRoller)
                GotRollers = true;
            else if (item.Definition.InteractionType == InteractionType.Moodlight)
            {
                if (_room.MoodlightData == null)
                    _room.MoodlightData = new(item.Id, _room.GetDatabase());
            }
            else if (item.Definition.InteractionType == InteractionType.Toner)
            {
                if (_room.TonerData == null)
                    _room.TonerData = new(item.Id, _room.GetDatabase());
            }
            else if (item.IsWired)
            {
                if (_room.GetWired() == null)
                    continue;

                _room.GetWired().LoadWiredBox(item);
            }
            else if (item.Definition.InteractionType == InteractionType.Hopper)
                HopperCount++;
        }
    }

    public Item GetItem(uint pId)
    {
        if (_floorItems.ContainsKey(pId))
        {
            if (_floorItems.TryGetValue(pId, out var item))
                return item;
        }
        else if (_wallItems.ContainsKey(pId))
        {
            if (_wallItems.TryGetValue(pId, out var item))
                return item;
        }
        return null!;
    }

    public void RemoveFurniture(GameClient? session, uint id)
    {
        var item = GetItem(id);
        if (item == null)
            return;
        if (item.Definition.InteractionType == InteractionType.FootballGate)
            _room.GetSoccer().UnRegisterGate(item);
        if (item.Definition.InteractionType != InteractionType.Gift)
            item.Interactor.OnRemove(session!, item);
        if (item.Definition.InteractionType == InteractionType.GuildGate)
        {
            item.UpdateCounter = 0;
            item.UpdateNeeded = false;
        }
        RemoveRoomItem(item);
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
            _rollerItemsMoved.Clear();
            _rollerUsersMoved.Clear();
            _rollerMessages.Clear();
            List<Item> itemsOnRoller;
            List<Item> itemsOnNext;
            foreach (var roller in _rollers.Values.ToList())
            {
                if (roller == null)
                    continue;
                var nextSquare = roller.SquareInFront;
                itemsOnRoller = _room.GetGameMap().GetRoomItemForSquare(roller.GetX, roller.GetY, roller.GetZ);
                itemsOnNext = _room.GetGameMap().GetAllRoomItemForSquare(nextSquare.X, nextSquare.Y).ToList();
                if (itemsOnRoller.Count > 10)
                    itemsOnRoller = _room.GetGameMap().GetRoomItemForSquare(roller.GetX, roller.GetY, roller.GetZ).Take(10).ToList();
                var nextSquareIsRoller = itemsOnNext.Count(x => x.Definition.InteractionType == InteractionType.Roller) > 0;
                var nextRollerClear = true;
                var nextZ = 0.0;
                var nextRoller = false;
                foreach (var item in itemsOnNext.ToList())
                {
                    if (item.IsRoller)
                    {
                        if (item.TotalHeight > nextZ)
                            nextZ = item.TotalHeight;
                        nextRoller = true;
                    }
                }
                if (nextRoller)
                {
                    foreach (var item in itemsOnNext.ToList())
                    {
                        if (item.TotalHeight > nextZ)
                            nextRollerClear = false;
                    }
                }
                if (itemsOnRoller.Count > 0)
                {
                    foreach (var rItem in itemsOnRoller.ToList())
                    {
                        if (rItem == null)
                            continue;
                        if (!_rollerItemsMoved.Contains(rItem.Id) && _room.GetGameMap().CanRollItemHere(nextSquare.X, nextSquare.Y) && nextRollerClear && roller.GetZ < rItem.GetZ &&
                            _room.GetRoomUserManager().GetUserForSquare(nextSquare.X, nextSquare.Y) == null)
                        {
                            if (!nextSquareIsRoller)
                                nextZ = rItem.GetZ - roller.Definition.Height;
                            else
                                nextZ = rItem.GetZ;
                            _rollerMessages.Add(UpdateItemOnRoller(rItem, nextSquare, roller.Id, nextZ));
                            _rollerItemsMoved.Add(rItem.Id);
                        }
                    }
                }
                var rollerUser = _room.GetGameMap().GetRoomUsers(roller.Coordinate).FirstOrDefault();
                if (rollerUser != null && !rollerUser.IsWalking && nextRollerClear &&
                    _room.GetGameMap().IsValidStep(new(roller.GetX, roller.GetY), new(nextSquare.X, nextSquare.Y), true, false, true) &&
                    _room.GetGameMap().CanRollItemHere(nextSquare.X, nextSquare.Y) && _room.GetGameMap().GetFloorStatus(nextSquare) != 0)
                {
                    if (!_rollerUsersMoved.Contains(rollerUser.HabboId))
                    {
                        if (!nextSquareIsRoller)
                            nextZ = rollerUser.Z - roller.Definition.Height;
                        else
                            nextZ = rollerUser.Z;
                        rollerUser.IsRolling = true;
                        rollerUser.RollerDelay = 1;
                        _rollerMessages.Add(UpdateUserOnRoller(rollerUser, nextSquare, roller.Id, nextZ));
                        _rollerUsersMoved.Add(rollerUser.HabboId);
                    }
                }
            }
            _mRollerCycle = 0;
            return _rollerMessages;
        }
        _mRollerCycle++;
        return new();
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
        if (habbo != null)
        {
            var items = _room.GetGameMap().GetRoomItemForSquare(pNextCoord.X, pNextCoord.Y);
            foreach (var item in items.ToList())
            {
                if (item == null)
                    continue;
                _room.GetWired().TriggerEvent(WiredBoxType.TriggerWalkOnFurni, habbo, item);
            }
            var roller = _room.GetRoomItemHandler().GetItem(pRollerId);
            if (roller != null) _room.GetWired().TriggerEvent(WiredBoxType.TriggerWalkOffFurni, habbo, roller);
        }
        return mMessage;
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
        if (item.Definition.InteractionType == InteractionType.Toner)
            if (_room.TonerData == null)
                _room.TonerData = new(item.Id, _room.GetDatabase());
        UpdateItem(item);
        _room.GetGameMap().AddItemToMap(item);
        return true;
    }

    public bool SetWallItem(GameClient session, Item item)
    {
        if (!item.IsWallItem || _wallItems.ContainsKey(item.Id))
            return false;
        if (_floorItems.ContainsKey(item.Id))
        {
            session.SendNotification(PlusEnvironment.LanguageManager.TryGetValue("room.item.already_placed"));
            return true;
        }
        item.Interactor.OnPlace(session, item);
        if (item.Definition.InteractionType == InteractionType.Moodlight)
        {
            if (_room.MoodlightData == null)
            {
                _room.MoodlightData = new(item.Id, _room.GetDatabase());
                item.LegacyDataString = _room.MoodlightData.GenerateExtraData();
            }
        }
        using (var connection = _room.GetDatabase().Connection())
        {
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
        _wallItems.TryAdd(item.Id, item);
        _room.SendPacket(new ItemAddComposer(item));
        return true;
    }

    public void UpdateItem(Item item)
    {
        if (item == null)
            return;
        if (!_movedItems.ContainsKey(item.Id))
            _movedItems.TryAdd(item.Id, item);
    }


    public void RemoveItem(Item item)
    {
        if (item == null)
            return;
        if (_movedItems.ContainsKey(item.Id))
            _movedItems.TryRemove(item.Id, out _);
        if (_rollers.ContainsKey(item.Id))
            _rollers.TryRemove(item.Id, out _);
    }

    public void OnCycle()
    {
        if (GotRollers)
        {
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
        if (_roomItemUpdateQueue.Count > 0)
        {
            var addItems = new List<Item>();
            while (_roomItemUpdateQueue.Count > 0)
            {
                Item? item = null;
                if (_roomItemUpdateQueue.TryDequeue(out item))
                {
                    if (item == null)
                        continue;
                    item.ProcessUpdates();
                    if (item.UpdateCounter > 0)
                        addItems.Add(item);
                }
            }
            foreach (var item in addItems.ToList())
            {
                if (item == null)
                    continue;
                _roomItemUpdateQueue.Enqueue(item);
            }
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
            if (item == null || item.UserId != habbo.Id)
                continue;
            if (item.IsFloorItem)
            {
                _floorItems.TryRemove(item.Id, out var I);
                // TODO @80O: Items refactor
                if (I != null)
                    inventory.AddItem(I.ToInventoryItem());
                _room.SendPacket(new ObjectRemoveComposer(item, item.UserId));
            }
            else if (item.IsWallItem)
            {
                _wallItems.TryRemove(item.Id, out var I);
                // TODO @80O: Items refactor
                if (I != null)
                    inventory.AddItem(I.ToInventoryItem());
                _room.SendPacket(new ItemRemoveComposer(item, item.UserId));
            }
            session.Send(new FurniListAddComposer(item.ToInventoryItem()));
        }
        _rollers.Clear();
        return items;
    }


    public bool CheckPosItem(Item item, int newX, int newY, int newRot)
    {
        try
        {
            var dictionary = Gamemap.GetAffectedTiles(item.Definition.Length, item.Definition.Width, newX, newY, newRot);
            if (!_room.GetGameMap().ValidTile(newX, newY))
                return false;
            foreach (var coord in dictionary.Values)
            {
                if (_room.GetGameMap().Model.DoorX == coord.X && _room.GetGameMap().Model.DoorY == coord.Y)
                    return false;
            }
            if (_room.GetGameMap().Model.DoorX == newX && _room.GetGameMap().Model.DoorY == newY)
                return false;
            foreach (var coord in dictionary.Values)
            {
                if (!_room.GetGameMap().ValidTile(coord.X, coord.Y))
                    return false;
            }
            double num = _room.GetGameMap().Model.SqFloorHeight[newX, newY];
            if (item.Rotation == newRot && item.GetX == newX && item.GetY == newY && item.GetZ != num)
                return false;
            if (_room.GetGameMap().Model.SqState[newX, newY] != SquareState.Open)
                return false;
            foreach (var coord in dictionary.Values)
            {
                if (_room.GetGameMap().Model.SqState[coord.X, coord.Y] != SquareState.Open)
                    return false;
            }
            if (!item.Definition.IsSeat)
            {
                if (_room.GetGameMap().SquareHasUsers(newX, newY))
                    return false;
                foreach (var coord in dictionary.Values)
                {
                    if (_room.GetGameMap().SquareHasUsers(coord.X, coord.Y))
                        return false;
                }
            }
            var furniObjects = GetFurniObjects(newX, newY);
            var collection = new List<Item>();
            var list3 = new List<Item>();
            foreach (var coord in dictionary.Values)
            {
                var list4 = GetFurniObjects(coord.X, coord.Y);
                if (list4 != null)
                    collection.AddRange(list4);
            }
            if (furniObjects == null)
                furniObjects = new();
            list3.AddRange(furniObjects);
            list3.AddRange(collection);
            foreach (var i in list3)
            {
                if (i.Id != item.Id && !i.Definition.Stackable)
                    return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }


    public ICollection<Item> GetRollers() => _rollers.Values;

    public void Dispose()
    {
        SaveFurniture();
        foreach (var item in GetWallAndFloor.ToList())
        {
            if (item == null)
                continue;
            item.Destroy();
        }
        _movedItems.Clear();
        _rollers.Clear();
        _wallItems.Clear();
        _floorItems.Clear();
        _rollerItemsMoved.Clear();
        _rollerUsersMoved.Clear();
        _rollerMessages.Clear();
        _roomItemUpdateQueue = null!;
    }
}
