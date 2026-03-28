using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class BotMovesToFurniBox : IWiredItem, IWiredEmptyExecutable
{
    public BotMovesToFurniBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.EffectBotMovesToFurniBox;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        var unknown = packet.ReadInt();
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
        if (string.IsNullOrEmpty(StringData))
            return false;
        var user = Instance.GetRoomUserManager().GetBotByName(StringData);
        if (user == null)
            return false;
        if (!WiredSetItemSelector.TryGetRandomFloorItem(Instance, SetItems, out var item))
            return false;
        if (Instance.GetGameMap() == null)
            return false;
        if (user.IsWalking) user.ClearMovement(true);
        user.BotData.ForcedMovement = true;
        user.BotData.TargetCoordinate = new(item.GetX, item.GetY);
        user.MoveTo(item.GetX, item.GetY);
        return true;
    }
}
