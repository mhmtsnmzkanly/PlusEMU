using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class TeleportBotToFurniBox : IWiredItem, IWiredEmptyExecutable
{
    public TeleportBotToFurniBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.EffectTeleportBotToFurniBox;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        _ = packet.ReadInt();
        var botName = packet.ReadString();
        if (SetItems.Count > 0)
            SetItems.Clear();
        var furniCount = packet.ReadInt();
        for (var i = 0; i < furniCount; i++)
        {
            var selectedItem = Instance.GetRoomItemHandler().GetItem(packet.ReadUInt());
            if (selectedItem != null)
                SetItems.TryAdd(selectedItem.Id, selectedItem);
        }
        StringData = botName;
    }

    bool IWiredEmptyExecutable.Execute(WiredEmptyExecutionContext context)
    {
        if (!WiredBotDataParser.TryParseBotName(StringData, out var botName))
            return false;
        if (!Instance.GetRoomUserManager().TryGetBotByName(botName, out var user) || user == null)
            return false;
        if (!WiredSetItemSelector.TryGetRandomFloorItem(Instance, SetItems, out var item))
            return false;
        if (Instance.GetGameMap() == null)
            return false;
        Instance.GetGameMap().TeleportToItem(user, item);
        Instance.GetRoomUserManager().UpdateUserStatusses();
        return true;
    }
}
