using System.Collections.Concurrent;
using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class KickUserBox : IWiredItem, IWiredCycle
{
    private const int KickDelay = 3;
    private readonly Queue<Habbo> _toKick;

    public KickUserBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
        TickCount = WiredCycleScheduler.GetTickCountForDelay(KickDelay);
        _toKick = new();
        if (SetItems.Count > 0)
            SetItems.Clear();
    }

    public int TickCount { get; set; }
    public int Delay { get; set; }

    public bool OnCycle()
    {
        if (Instance == null)
            return false;
        if (_toKick.Count == 0)
        {
            TickCount = KickDelay;
            return true;
        }
        lock (_toKick)
        {
            while (_toKick.Count > 0)
            {
                var player = _toKick.Dequeue();
                if (player == null || !player.InRoom || player.CurrentRoom != Instance || player.Client == null)
                    continue;
                Instance.GetRoomUserManager().RemoveUserFromRoom(player.Client, true);
            }
        }
        TickCount = KickDelay;
        return true;
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.EffectKickUser;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        if (SetItems.Count > 0)
            SetItems.Clear();
        var unknown = packet.ReadInt();
        var message = packet.ReadString();
        StringData = message;
    }

    public bool Execute(params object[] @params)
    {
        if (@params.Length != 1)
            return false;
        var player = (Habbo)@params[0];
        if (player == null)
            return false;
        if (TickCount <= 0)
            TickCount = KickDelay;
        if (!_toKick.Contains(player))
        {
            var user = Instance.GetRoomUserManager().GetRoomUserByHabbo(player.Id);
            var playerClient = player.Client;
            if (user == null || playerClient == null)
                return false;
            if ((player.Permissions?.HasRight("mod_tool") ?? false) || Instance.OwnerId == player.Id)
            {
                playerClient.Send(new WhisperComposer(user.VirtualId, "Wired Kick Exception: Unkickable Player", 0, 0));
                return false;
            }
            _toKick.Enqueue(player);
            playerClient.Send(new WhisperComposer(user.VirtualId, StringData, 0, 0));
        }
        return true;
    }
}
