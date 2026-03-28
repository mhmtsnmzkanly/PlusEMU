using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Rooms.Games.Teams;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class AddActorToTeamBox : IWiredItem
{
    public AddActorToTeamBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.EffectAddActorToTeam;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        var unknown = packet.ReadInt();
        var team = packet.ReadInt();
        StringData = team.ToString();
    }

    public bool Execute(params object[] @params)
    {
        if (Instance == null || string.IsNullOrEmpty(StringData))
            return false;
        if (!WiredContextResolver.TryGetActor(@params, out var player))
            return false;
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
        if (!WiredTeamParser.TryParseTeam(StringData, out var toJoin))
            return false;
        var team = Instance.GetTeamManagerForFreeze();
        if (team != null)
        {
            if (team.CanEnterOnTeam(toJoin))
            {
                if (user.Team != Team.None)
                    team.OnUserLeave(user);
                user.Team = toJoin;
                team.AddUser(user);
                var effectId = WiredTeamParser.GetEffectId(toJoin);
                if (effects.CurrentEffect != effectId)
                    effects.ApplyEffect(effectId);
            }
        }
        return true;
    }
}
