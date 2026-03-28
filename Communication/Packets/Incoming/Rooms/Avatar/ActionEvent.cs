using Plus.Communication.Packets.Outgoing.Rooms.Avatar;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Avatar;

public class ActionEvent : RoomPacketEvent
{
    private readonly IQuestService _questService;

    public ActionEvent(IQuestService questService)
    {
        _questService = questService;
    }

    public override async Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { Effects: { } effects } habbo)
            return;

        var action = packet.ReadInt();
        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null)
            return;

        if (user.DanceId > 0)
            user.DanceId = 0;

        if (effects.CurrentEffect > 0)
            room.SendPacket(new AvatarEffectComposer(user.VirtualId, 0));

        user.UnIdle();
        room.SendPacket(new ActionComposer(user.VirtualId, action));
        if (action == 5) // idle
        {
            user.IsAsleep = true;
            room.SendPacket(new SleepComposer(user, true));
        }

        await _questService.ProgressUserQuest(session, QuestType.SocialWave);
    }
}
