using System.Globalization;
using Plus.HabboHotel.Cache;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Outgoing.Rooms.Engine;

public class UsersComposer : IServerPacket
{
    private readonly ICollection<RoomUser> _users;
    private readonly IGroupManager _groupManager;
    private readonly ICacheManager _cacheManager;

    public uint MessageId => ServerPacketHeader.UsersComposer;

    public UsersComposer(ICollection<RoomUser> users, IGroupManager groupManager, ICacheManager cacheManager)
    {
        _users = users;
        _groupManager = groupManager;
        _cacheManager = cacheManager;
    }

    public UsersComposer(RoomUser user, IGroupManager groupManager, ICacheManager cacheManager)
    {
        _users = new List<RoomUser>() { user };
        _groupManager = groupManager;
        _cacheManager = cacheManager;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_users.Count);
        foreach (var user in _users.ToList()) WriteUser(packet, user);
    }

    private void WriteUser(IOutgoingPacket packet, RoomUser user)
    {
        if (!user.IsPet && !user.IsBot)
        {
            var client = user.GetClient();
            var habbo = client?.GetHabbo();
            if (habbo == null)
                return;

            Group? group = null;
            if (habbo.HabboStats != null)
            {
                if (habbo.HabboStats.FavouriteGroupId > 0)
                {
                    if (!_groupManager.TryGetGroup(habbo.HabboStats.FavouriteGroupId, out group))
                        group = null;
                }
            }
            packet.WriteInteger(habbo.Id);
            packet.WriteString(habbo.Username ?? string.Empty);
            packet.WriteString(habbo.Motto ?? string.Empty);
            packet.WriteString(habbo.Look ?? string.Empty);
            packet.WriteInteger(user.VirtualId);
            packet.WriteInteger(user.X);
            packet.WriteInteger(user.Y);
            packet.WriteString(user.Z.ToString(CultureInfo.InvariantCulture));
            packet.WriteInteger(user.RotBody); //2 for user, 4 for bot.
            packet.WriteInteger(1); //1 for user, 2 for pet, 3 for bot.
            packet.WriteString((habbo.Gender ?? string.Empty).ToLower());
            if (group != null)
            {
                packet.WriteInteger(group.Id);
                packet.WriteInteger(0);
                packet.WriteString(group.Name ?? string.Empty);
            }
            else
            {
                packet.WriteInteger(0);
                packet.WriteInteger(0);
                packet.WriteString("");
            }
            packet.WriteString(""); //Swim Figure
            packet.WriteInteger(habbo.HabboStats?.AchievementPoints ?? 0); //Achievement score
            packet.WriteBoolean(false); //Is Moderator
        }
        else if (user.IsPet)
        {
            packet.WriteInteger(user.BotAi.BaseId);
            packet.WriteString(user.BotData.Name ?? string.Empty);
            packet.WriteString(user.BotData.Motto ?? string.Empty);

            packet.WriteString((user.BotData.Look ?? string.Empty).ToLower() + (user.PetData.Saddle > 0
                ? $" 3 2 {user.PetData.PetHair} {user.PetData.HairDye} 3 {user.PetData.PetHair} {user.PetData.HairDye} 4 {user.PetData.Saddle} 0"
                : $" 2 2 {user.PetData.PetHair} {user.PetData.HairDye} 3 {user.PetData.PetHair} {user.PetData.HairDye}"));
            packet.WriteInteger(user.VirtualId);
            packet.WriteInteger(user.X);
            packet.WriteInteger(user.Y);
            packet.WriteString(user.Z.ToString(CultureInfo.InvariantCulture));
            packet.WriteInteger(0);
            packet.WriteInteger(user.BotData.AiType == BotAiType.Pet ? 2 : 4);
            packet.WriteInteger(user.PetData.Type);
            packet.WriteInteger(user.PetData.OwnerId); // userid
            packet.WriteString(user.PetData.OwnerName ?? string.Empty); // username
            packet.WriteInteger(1);
            packet.WriteBoolean(user.PetData.Saddle > 0);
            packet.WriteBoolean(user.RidingHorse);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteString("");
        }
        else if (user.IsBot)
        {
            packet.WriteInteger(user.BotAi.BaseId);
            packet.WriteString(user.BotData.Name ?? string.Empty);
            packet.WriteString(user.BotData.Motto ?? string.Empty);
            packet.WriteString((user.BotData.Look ?? string.Empty).ToLower());
            packet.WriteInteger(user.VirtualId);
            packet.WriteInteger(user.X);
            packet.WriteInteger(user.Y);
            packet.WriteString(user.Z.ToString(CultureInfo.InvariantCulture));
            packet.WriteInteger(0);
            packet.WriteInteger(user.BotData.AiType == BotAiType.Pet ? 2 : 4);
            packet.WriteString((user.BotData.Gender ?? string.Empty).ToLower()); // ?
            packet.WriteInteger(user.BotData.OwnerId); //Owner Id
            packet.WriteString(_cacheManager.GenerateUser(user.BotData.OwnerId)?.Username ?? "Unknown User"); // Owner name
            packet.WriteInteger(5); //Action Count
            packet.WriteShort(1); //Copy looks
            packet.WriteShort(2); //Setup speech
            packet.WriteShort(3); //Relax
            packet.WriteShort(4); //Dance
            packet.WriteShort(5); //Change name
        }
    }
}
