using System.Collections.Concurrent;
using Dapper;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class BotChangesClothesBox : IWiredItem, IWiredEmptyExecutable
{
    public BotChangesClothesBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.EffectBotChangesClothesBox;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        _ = packet.ReadInt();
        var botConfiguration = packet.ReadString();
        if (SetItems.Count > 0)
            SetItems.Clear();
        StringData = botConfiguration;
    }

    bool IWiredEmptyExecutable.Execute(WiredEmptyExecutionContext context)
    {
        if (!WiredBotDataParser.TryParseBotClothing(StringData, out var botName, out var figure))
            return false;
        var user = Instance.GetRoomUserManager().GetBotByName(botName);
        if (user == null)
            return false;
        var userChangeComposer = new UserChangeComposer(user.BotData);
        Instance.SendPacket(userChangeComposer);
        user.BotData.Look = figure;
        user.BotData.Gender = "M";
        using var db = Instance.GetDatabase().Connection();
        db.Execute(
            "UPDATE `bots` SET `look` = @look, `gender` = @gender WHERE `id` = @id LIMIT 1",
            new { look = user.BotData.Look, gender = user.BotData.Gender, id = user.BotData.Id });
        return true;
    }
}
