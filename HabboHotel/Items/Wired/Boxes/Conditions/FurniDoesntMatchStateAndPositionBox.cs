using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Wired.Boxes.Conditions;

internal class FurniDoesntMatchStateAndPositionBox : IWiredItem, IWiredExecutable
{
    public FurniDoesntMatchStateAndPositionBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
        StringData = string.Empty;
        ItemsData = string.Empty;
    }

    public Room Instance { get; set; }

    public Item Item { get; set; }

    public WiredBoxType Type => WiredBoxType.ConditionDontMatchStateAndPosition;

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

    bool IWiredExecutable.Execute(WiredExecutionContext context)
    {
        if (string.IsNullOrEmpty(StringData) || StringData == "0;0;0" || SetItems.Count == 0)
            return false;
        if (!WiredConditionDataParser.TryParseStatePositionModes(StringData, out var stateMode, out var directionMode, out var positionMode))
            return false;
        foreach (var item in SetItems.Values.ToList())
        {
            if (!Instance.GetRoomItemHandler().GetFloor.Contains(item))
                continue;
            foreach (var I in ItemsData.Split(';'))
            {
                if (string.IsNullOrEmpty(I))
                    continue;
                if (!WiredFurniSnapshotParser.TryParseEntry(I, out var itemId, out var snapshot))
                    continue;
                var ii = Instance.GetRoomItemHandler().GetItem(itemId);
                if (ii == null)
                    continue;
                if (stateMode == 1) //State
                {
                    if (ii.LegacyDataString == snapshot.State)
                        return false;
                }
                if (directionMode == 1) //Direction
                {
                    if (ii.Rotation == snapshot.Rotation)
                        return false;
                }
                if (positionMode == 1) //Position
                {
                    if (ii.GetX == snapshot.X && ii.GetY == snapshot.Y && ii.GetZ == snapshot.Z)
                        return false;
                }
            }
        }
        return true;
    }
}
