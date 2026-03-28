using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class BotCommunicatesToAllBox : IWiredItem, IWiredExecutable, IWiredEmptyExecutable
{
    public BotCommunicatesToAllBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.EffectBotCommunicatesToAllBox;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        var unknown = packet.ReadInt();
        var chatMode = packet.ReadInt();
        var chatConfig = packet.ReadString();
        if (SetItems.Count > 0)
            SetItems.Clear();

        //this.StringData = ChatConfig.Replace('\t', ';') + ";" + ChatMode;
    }

    bool IWiredEmptyExecutable.Execute(WiredEmptyExecutionContext context)
    {
        if (string.IsNullOrEmpty(StringData))
            return false;
        var user = Instance.GetRoomUserManager().GetBotByName(StringData);
        if (user == null)
            return false;

        //TODO: This needs finishing.
        return true;
    }
}
