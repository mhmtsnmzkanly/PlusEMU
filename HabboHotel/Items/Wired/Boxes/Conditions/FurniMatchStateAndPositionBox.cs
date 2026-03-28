using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Wired.Boxes.Conditions;

internal class FurniMatchStateAndPositionBox : IWiredItem, IWiredEmptyExecutable
{
    public FurniMatchStateAndPositionBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public Room Instance { get; set; }

    public Item Item { get; set; }

    public WiredBoxType Type => WiredBoxType.ConditionMatchStateAndPosition;

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
    }

    bool IWiredEmptyExecutable.Execute(WiredEmptyExecutionContext context)
    {
        if (string.IsNullOrEmpty(StringData) || StringData == "0;0;0" || SetItems.Count == 0)
            return false;
        if (!WiredConditionDataParser.TryParseStatePositionModes(StringData, out var stateMode, out var directionMode, out var positionMode))
            return false;
        foreach (var item in SetItems.Values.ToList())
        {
            if (item == null)
                continue;
            if (!Instance.GetRoomItemHandler().GetFloor.Contains(item))
                continue;
            foreach (var entry in WiredFurniSnapshotParser.EnumerateEntries(ItemsData))
            {
                var ii = Instance.GetRoomItemHandler().GetItem(entry.ItemId);
                if (ii == null)
                    continue;
                if (stateMode == 1) //State
                {
                    try
                    {
                        if (ii.LegacyDataString != entry.Snapshot.State)
                            return false;
                    }
                    catch { }
                }
                if (directionMode == 1) //Direction
                {
                    try
                    {
                        if (ii.Rotation != entry.Snapshot.Rotation)
                            return false;
                    }
                    catch { }
                }
                if (positionMode == 1) //Position
                {
                    try
                    {
                        if (ii.GetX != entry.Snapshot.X || ii.GetY != entry.Snapshot.Y || ii.GetZ != entry.Snapshot.Z)
                            return false;
                    }
                    catch { }
                }
            }
        }
        return true;
    }
}
