using Plus.Communication.Packets.Outgoing.Rooms.Avatar;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Avatar;

internal class DanceEvent : RoomPacketEvent
{
    private readonly IQuestService _questService;

    public DanceEvent(IQuestService questService)
    {
        _questService = questService;
    }

    public override async Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Effects == null)
            return;
        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null)
            return;
        user.UnIdle();
        var danceId = packet.ReadInt();
        if (danceId < 0 || danceId > 4)
            danceId = 0;
        if (danceId > 0 && user.CarryItemId > 0)
            user.CarryItem(0);
        if (habbo.Effects.CurrentEffect > 0)
            room.SendPacket(new AvatarEffectComposer(user.VirtualId, 0));
        user.DanceId = danceId;
        room.SendPacket(new DanceComposer(user, danceId));
        await _questService.ProgressUserQuest(session, QuestType.SocialDance);
        if (room.GetRoomUserManager().GetRoomUsers().Count > 19)
            await _questService.ProgressUserQuest(session, QuestType.MassDance);
    }
}
