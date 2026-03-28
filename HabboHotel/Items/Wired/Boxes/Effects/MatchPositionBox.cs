using System.Collections.Concurrent;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Core;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Items.Wired;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class MatchPositionBox : IWiredItem, IWiredCycle, IWiredExecutable
{
    private int _delay;

    private bool _requested;

    public MatchPositionBox(Room instance, Item item)
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
        if (!_requested || string.IsNullOrEmpty(StringData) || StringData == "0;0;0" || SetItems.Count == 0)
            return false;
        if (!TryParseModes(out var stateMode, out var directionMode, out var positionMode))
        {
            _requested = false;
            return false;
        }
        foreach (var item in SetItems.Values.ToList())
        {
            if (Instance.GetRoomItemHandler().GetFloor == null || !Instance.GetRoomItemHandler().GetFloor.Contains(item))
                continue;
            foreach (var entry in ItemsData.Split(';'))
            {
                if (string.IsNullOrEmpty(entry))
                    continue;
                if (!WiredFurniSnapshotParser.TryParseEntry(entry, out var itemId, out var snapshot))
                    continue;
                var targetItem = Instance.GetRoomItemHandler().GetItem(itemId);
                if (targetItem == null)
                    continue;
                if (stateMode == 1)
                    SetState(targetItem, snapshot.State);
                if (directionMode == 1)
                {
                    try
                    {
                        SetRotation(targetItem, snapshot.Rotation);
                    }
                    catch (Exception e)
                    {
                        ExceptionLogger.LogWiredException(e);
                    }
                }
                if (positionMode == 1)
                {
                    try
                    {
                        SetPosition(targetItem, snapshot.X, snapshot.Y, snapshot.Z);
                    }
                    catch (Exception e)
                    {
                        ExceptionLogger.LogWiredException(e);
                    }
                }
            }
        }
        _requested = false;
        return true;
    }

    public Room Instance { get; set; }

    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.EffectMatchPosition;

    public ConcurrentDictionary<uint, Item> SetItems { get; set; }

    public string StringData { get; set; } = string.Empty;

    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        if (SetItems.Count > 0)
            SetItems.Clear();
        var unknown = packet.ReadInt();
        var state = packet.ReadInt();
        var direction = packet.ReadInt();
        var placement = packet.ReadInt();
        var unknown2 = packet.ReadString();
        var furniCount = packet.ReadInt();
        for (var i = 0; i < furniCount; i++)
        {
            var selectedItem = Instance.GetRoomItemHandler().GetItem(packet.ReadUInt());
            if (selectedItem != null)
                SetItems.TryAdd(selectedItem.Id, selectedItem);
        }
        StringData = $"{state};{direction};{placement}";
        var delay = packet.ReadInt();
        Delay = delay;
    }

    public bool Execute(params object[] @params)
    {
        return ((IWiredExecutable)this).Execute(new(@params));
    }

    bool IWiredExecutable.Execute(WiredExecutionContext context)
    {
        if (WiredCycleScheduler.MarkRequested(ref _requested))
        {
            TickCount = Delay;
        }
        return true;
    }

    private bool TryParseModes(out int stateMode, out int directionMode, out int positionMode)
    {
        stateMode = 0;
        directionMode = 0;
        positionMode = 0;

        var modeParts = StringData.Split(';');
        return modeParts.Length >= 3 &&
               int.TryParse(modeParts[0], out stateMode) &&
               int.TryParse(modeParts[1], out directionMode) &&
               int.TryParse(modeParts[2], out positionMode);
    }

    private void SetState(Item item, string extradata)
    {
        if (item.LegacyDataString == extradata)
            return;
        if (item.Definition.InteractionType == InteractionType.Dice)
            return;
        item.LegacyDataString = extradata;
        item.UpdateState(false, true);
    }

    private void SetRotation(Item item, int rotation)
    {
        if (item.Rotation == rotation)
            return;
        item.Rotation = rotation;
        item.UpdateState(false, true);
    }

    private void SetPosition(Item item, int coordX, int coordY, double coordZ)
    {
        Instance.SendPacket(new SlideObjectBundleComposer(item.GetX, item.GetY, item.GetZ, coordX, coordY, coordZ, 0, 0, item.Id));
        Instance.GetRoomItemHandler().SetFloorItem(item, coordX, coordY, coordZ);
        //Instance.GetGameMap().GenerateMaps();
    }
}
