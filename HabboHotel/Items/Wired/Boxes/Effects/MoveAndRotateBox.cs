using System.Collections.Concurrent;
using System.Drawing;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Items.Wired;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class MoveAndRotateBox : IWiredItem, IWiredCycle, IWiredEmptyExecutable
{
    private int _delay;
    private long _next;
    private bool _requested;

    public MoveAndRotateBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
        TickCount = Delay;
        _requested = false;
    }

    public int Delay
    {
        get => _delay;
        set
        {
            _delay = value;
            TickCount = WiredCycleScheduler.GetTickCountForDelay(value, extraTick: true);
        }
    }

    public int TickCount { get; set; }

    public bool OnCycle()
    {
        if (Instance == null || !_requested || _next == 0)
            return false;
        if (!WiredEffectDataParser.TryParseMoveAndRotateModes(StringData, out var movementMode, out var rotationMode))
            return false;
        if (WiredCycleScheduler.IsReady(_requested, _next))
        {
            foreach (var item in SetItems.Values.ToList())
            {
                if (item == null)
                    continue;
                if (!Instance.GetRoomItemHandler().GetFloor.Contains(item))
                    continue;
                if (Instance.GetWired().OtherBoxHasItem(this, item.Id))
                {
                    SetItems.TryRemove(item.Id, out _);
                    continue;
                }
                var point = HandleMovement(movementMode, new(item.GetX, item.GetY));
                var newRot = HandleRotation(rotationMode, item.Rotation);
                Instance.GetWired().OnUserFurniCollision(Instance, item);
                if (newRot != item.Rotation)
                {
                    item.Rotation = newRot;
                    item.UpdateState(false, true);
                }

                if (!WiredFloorMoveHelper.TryMoveFloorItem(
                        Instance,
                        item,
                        point,
                        out _,
                        () => Instance.GetGameMap().GetHeightForSquareFromData(point)))
                    _next = 0;
            }
            _next = 0;
            return true;
        }
        return false;
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }

    public WiredBoxType Type => WiredBoxType.EffectMoveAndRotate;

    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        if (SetItems.Count > 0)
            SetItems.Clear();
        var unknown = packet.ReadInt();
        var movement = packet.ReadInt();
        var rotation = packet.ReadInt();
        var unknown1 = packet.ReadString();
        var furniCount = packet.ReadInt();
        for (var i = 0; i < furniCount; i++)
        {
            var selectedItem = Instance.GetRoomItemHandler().GetItem(packet.ReadUInt());
            if (selectedItem != null && !Instance.GetWired().OtherBoxHasItem(this, selectedItem.Id))
                SetItems.TryAdd(selectedItem.Id, selectedItem);
        }
        StringData = $"{movement};{rotation}";
        Delay = packet.ReadInt();
    }

    bool IWiredEmptyExecutable.Execute(WiredEmptyExecutionContext context)
    {
        if (SetItems.Count == 0)
            return false;
        if (WiredCycleScheduler.Schedule(ref _next, ref _requested, Delay))
        {
            TickCount = Delay;
        }
        return true;
    }

    private int HandleRotation(int mode, int rotation)
    {
        switch (mode)
        {
            case 1:
            {
                rotation += 2;
                if (rotation > 6) rotation = 0;
                break;
            }
            case 2:
            {
                rotation -= 2;
                if (rotation < 0) rotation = 6;
                break;
            }
            case 3:
            {
                if (Random.Shared.Next(0, 3) == 0)
                {
                    rotation += 2;
                    if (rotation > 6) rotation = 0;
                }
                else
                {
                    rotation -= 2;
                    if (rotation < 0) rotation = 6;
                }
                break;
            }
        }
        return rotation;
    }

    private Point HandleMovement(int mode, Point position)
    {
        var newPos = new Point();
        switch (mode)
        {
            case 0:
            {
                newPos = position;
                break;
            }
            case 1:
            {
                switch (Random.Shared.Next(1, 5))
                {
                    case 1:
                        newPos = new(position.X + 1, position.Y);
                        break;
                    case 2:
                        newPos = new(position.X - 1, position.Y);
                        break;
                    case 3:
                        newPos = new(position.X, position.Y + 1);
                        break;
                    case 4:
                        newPos = new(position.X, position.Y - 1);
                        break;
                }
                break;
            }
            case 2:
            {
                if (Random.Shared.Next(0, 3) == 1)
                    newPos = new(position.X - 1, position.Y);
                else
                    newPos = new(position.X + 1, position.Y);
                break;
            }
            case 3:
            {
                if (Random.Shared.Next(0, 3) == 1)
                    newPos = new(position.X, position.Y - 1);
                else
                    newPos = new(position.X, position.Y + 1);
                break;
            }
            case 4:
            {
                newPos = new(position.X, position.Y - 1);
                break;
            }
            case 5:
            {
                newPos = new(position.X + 1, position.Y);
                break;
            }
            case 6:
            {
                newPos = new(position.X, position.Y + 1);
                break;
            }
            case 7:
            {
                newPos = new(position.X - 1, position.Y);
                break;
            }
        }
        return newPos;
    }
}
