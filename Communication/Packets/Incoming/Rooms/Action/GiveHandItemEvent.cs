using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class GiveHandItemEvent : RoomPacketEvent
{
    private readonly IQuestManager _questManager;

    public GiveHandItemEvent(IQuestManager questManager)
    {
        _questManager = questManager;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null)
            return Task.CompletedTask;
        var targetUser = room.GetRoomUserManager().GetRoomUserByHabbo(packet.ReadInt());
        if (targetUser == null)
            return Task.CompletedTask;
        if (!(Math.Abs(user.X - targetUser.X) >= 3 || Math.Abs(user.Y - targetUser.Y) >= 3) || (habbo.Permissions?.HasRight("mod_tool") ?? false))
        {
            if (user.CarryItemId > 0 && user.CarryTimer > 0)
            {
                if (user.CarryItemId == 8)
                    _questManager.ProgressUserQuest(session, QuestType.GiveCoffee);
                targetUser.CarryItem(user.CarryItemId);
                user.CarryItem(0);
                targetUser.DanceId = 0;
            }
        }
        return Task.CompletedTask;
    }
}
