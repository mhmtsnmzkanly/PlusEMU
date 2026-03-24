using Plus.Communication.Packets.Outgoing.Pets;
using Plus.Communication.Packets.Outgoing.Rooms.Avatar;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Pets;

internal class RespectPetEvent : RoomPacketEvent
{
    private readonly IAchievementManager _achievementManager;
    private readonly IQuestManager _questManager;

    public RespectPetEvent(IAchievementManager achievementManager, IQuestManager questManager)
    {
        _achievementManager = achievementManager;
        _questManager = questManager;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.HabboStats == null || !habbo.InRoom || habbo.HabboStats.DailyPetRespectPoints == 0)
            return Task.CompletedTask;
        var currentRoom = habbo.CurrentRoom;
        if (currentRoom == null)
            return Task.CompletedTask;
        var thisUser = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (thisUser == null)
            return Task.CompletedTask;
        var petId = packet.ReadInt();
        if (!currentRoom.GetRoomUserManager().TryGetPet(petId, out var pet))
        {
            //Okay so, we've established we have no pets in this room by this virtual Id, let us check out users, maybe they're creeping as a pet?!
            var targetUser = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(petId);
            if (targetUser == null)
                return Task.CompletedTask;

            //Check some values first, please!
            var targetClient = targetUser.GetClient();
            var targetHabbo = targetClient?.GetHabbo();
            if (targetHabbo?.HabboStats == null)
                return Task.CompletedTask;
            if (targetHabbo.Id == habbo.Id)
            {
                session.SendWhisper("Oops, you cannot use this on yourself! (You haven't lost a point, simply reload!)");
                return Task.CompletedTask;
            }

            //And boom! Let us send some respect points.
            _questManager.ProgressUserQuest(session, QuestType.SocialRespect);
            _achievementManager.ProgressAchievement(session, "ACH_RespectGiven", 1);
            if (targetClient == null)
                return Task.CompletedTask;
            _achievementManager.ProgressAchievement(targetClient, "ACH_RespectEarned", 1);

            //Take away from pet respect points, just in-case users abuse this..
            habbo.HabboStats.DailyPetRespectPoints -= 1;
            habbo.HabboStats.RespectGiven += 1;
            targetHabbo.HabboStats.Respect += 1;

            //Apply the effect.
            thisUser.CarryItemId = 999999999;
            thisUser.CarryTimer = 5;

            //Send the magic out.
            if (room.RespectNotificationsEnabled)
                room.SendPacket(new RespectPetNotificationComposer(targetHabbo, targetUser));
            room.SendPacket(new CarryObjectComposer(thisUser.VirtualId, thisUser.CarryItemId));
            return Task.CompletedTask;
        }
        if (pet == null || pet.PetData == null || pet.RoomId != currentRoom.RoomId)
            return Task.CompletedTask;
        habbo.HabboStats.DailyPetRespectPoints -= 1;
        _achievementManager.ProgressAchievement(session, "ACH_PetRespectGiver", 1);
        thisUser.CarryItemId = 999999999;
        thisUser.CarryTimer = 5;
        pet.PetData.OnRespect();
        room.SendPacket(new CarryObjectComposer(thisUser.VirtualId, thisUser.CarryItemId));
        return Task.CompletedTask;
    }
}
