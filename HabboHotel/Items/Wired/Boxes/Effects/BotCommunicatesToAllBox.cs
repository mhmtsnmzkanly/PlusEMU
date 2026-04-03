using System.Collections.Concurrent;
using Plus.Communication.Packets;
using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class BotCommunicatesToAllBox : IWiredItem, IWiredEmptyExecutable
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
        _ = packet.ReadInt();
        var chatMode = packet.ReadInt();
        var chatConfig = packet.ReadString();
        if (SetItems.Count > 0)
            SetItems.Clear();
        StringData = $"{chatConfig};{chatMode}";
    }

    bool IWiredEmptyExecutable.Execute(WiredEmptyExecutionContext context)
    {
        if (!WiredBotDataParser.TryParseBotCommunication(StringData, out var botName, out var message, out var chatMode))
            return false;
        if (!Instance.GetRoomUserManager().TryGetBotByName(botName, out var user) || user == null)
            return false;
        SpeakToRoom(user, message, chatMode);
        return true;
    }

    private void SpeakToRoom(RoomUser bot, string message, int chatMode)
    {
        var roomUserManager = Instance.GetRoomUserManager();
        var bubbleId = bot.BotData?.ChatBubble ?? 0;
        var packet = CreateSpeechPacket(bot.VirtualId, message, bubbleId, chatMode);

        foreach (var user in roomUserManager.GetUserList().ToList())
        {
            if (user == null || user.IsBot)
                continue;

            var client = user.GetClient();
            if (client == null)
                continue;
            var habbo = client.GetHabbo();
            if (habbo == null)
                continue;

            if (!habbo.AllowBotSpeech)
                client.Send(packet);
        }

        foreach (var user in roomUserManager.GetUserList().ToList())
        {
            if (user == null || !user.IsBot)
                continue;

            if (chatMode == 1)
                user.BotAi.OnUserShout(bot, message);
            else
                user.BotAi.OnUserSay(bot, message);
        }
    }

    private static IServerPacket CreateSpeechPacket(int virtualId, string message, int bubbleId, int chatMode) =>
        chatMode == 1
            ? new ShoutComposer(virtualId, message, 0, bubbleId == 0 ? 2 : bubbleId)
            : new ChatComposer(virtualId, message, 0, bubbleId == 0 ? 2 : bubbleId);
}
