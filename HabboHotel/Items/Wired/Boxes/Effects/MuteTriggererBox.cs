using System.Collections.Concurrent;
using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;
using Plus.Utilities;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class MuteTriggererBox : IWiredItem, IWiredActorExecutable
{
    public MuteTriggererBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
        if (SetItems.Count > 0)
            SetItems.Clear();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.EffectMuteTriggerer;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        if (SetItems.Count > 0)
            SetItems.Clear();
        _ = packet.ReadInt();
        var time = packet.ReadInt();
        var message = packet.ReadString();
        StringData = $"{time};{message}";
    }

    bool IWiredActorExecutable.Execute(WiredActorExecutionContext context)
    {
        var player = context.Actor;
        if (player == null || !player.TryGetClient(out var playerClient))
            return false;
        if (!Instance.GetRoomUserManager().TryGetRoomUserByHabbo(player.Id, out var user) || user == null)
            return false;
        if ((player.Permissions?.HasRight("mod_tool") ?? false) || Instance.OwnerId == player.Id)
        {
            playerClient.Send(new WhisperComposer(user.VirtualId, Instance.GetLanguageManager().Require("wired.mute.exception_unmutable"), 0, 0));
            return false;
        }
        if (!WiredEffectDataParser.TryParseMute(StringData, out var time, out var message))
            return false;
        if (time > 0)
        {
            playerClient.Send(new WhisperComposer(user.VirtualId, Instance.GetLanguageManager().Format("wired.mute.applied", ("time", time.ToString()), ("message", message)), 0, 0));
            if (!Instance.MutedUsers.ContainsKey(player.Id))
                Instance.MutedUsers.Add(player.Id, UnixTimestamp.GetNow() + time * 60);
            else
            {
                Instance.MutedUsers.Remove(player.Id);
                Instance.MutedUsers.Add(player.Id, UnixTimestamp.GetNow() + time * 60);
            }
        }
        return true;
    }
}
