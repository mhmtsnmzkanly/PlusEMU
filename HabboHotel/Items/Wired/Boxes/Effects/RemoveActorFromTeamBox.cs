using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Rooms.Games.Teams;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class RemoveActorFromTeamBox : IWiredItem, IWiredExecutable, IWiredActorExecutable
{
    public RemoveActorFromTeamBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.EffectRemoveActorFromTeam;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        var unknown = packet.ReadInt();
    }

    bool IWiredExecutable.Execute(WiredExecutionContext context)
    {
        return ((IWiredActorExecutable)this).Execute((WiredActorExecutionContext)context);
    }

    bool IWiredActorExecutable.Execute(WiredActorExecutionContext context)
    {
        if (Instance == null)
            return false;
        var player = context.Actor;
        if (player == null)
            return false;
        var user = Instance.GetRoomUserManager().GetRoomUserByHabbo(player.Id);
        if (user == null)
            return false;
        var client = user.GetClient();
        var habbo = client?.GetHabbo();
        var effects = habbo?.Effects;
        if (effects == null)
            return false;
        if (user.Team != Team.None)
        {
            var team = Instance.GetTeamManagerForFreeze();
            if (team != null)
            {
                team.OnUserLeave(user);
                user.Team = Team.None;
                if (effects.CurrentEffect != 0)
                    effects.ApplyEffect(0);
            }
        }
        return true;
    }
}
