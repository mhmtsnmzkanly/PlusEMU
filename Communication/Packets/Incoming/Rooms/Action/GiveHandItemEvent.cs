using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class GiveHandItemEvent : RoomPacketEvent
{
    private readonly IQuestService _questService;

    public GiveHandItemEvent(IQuestService questService)
    {
        _questService = questService;
    }

    public override async Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo)
            return;

        if (!room.GetRoomUserManager().TryGetRoomUserByHabbo(habbo.Id, out var user) || user == null)
            return;

        if (!room.GetRoomUserManager().TryGetRoomUserByHabbo(packet.ReadInt(), out var targetUser) || targetUser == null)
            return;

        if (!(Math.Abs(user.X - targetUser.X) >= 3 || Math.Abs(user.Y - targetUser.Y) >= 3) || (habbo.Permissions?.HasRight("mod_tool") ?? false))
        {
            if (user.CarryItemId > 0 && user.CarryTimer > 0)
            {
                if (user.CarryItemId == 8)
                    await _questService.ProgressUserQuest(session, QuestType.GiveCoffee);
                targetUser.CarryItem(user.CarryItemId);
                user.CarryItem(0);
                targetUser.DanceId = 0;
            }
        }
    }
}
