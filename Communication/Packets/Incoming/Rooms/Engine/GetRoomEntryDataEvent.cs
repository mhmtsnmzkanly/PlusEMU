using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Quests;
using Plus.Utilities;

namespace Plus.Communication.Packets.Incoming.Rooms.Engine;

internal class GetRoomEntryDataEvent : IPacketEvent
{
    private readonly IQuestService _questService;

    public GetRoomEntryDataEvent(IQuestService questService)
    {
        _questService = questService;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var room = habbo?.CurrentRoom;
        if (room == null)
            return;
        if (!room.GetRoomUserManager().AddAvatarToRoom(session))
        {
            room.GetRoomUserManager().RemoveUserFromRoom(session, false);
            return; //TODO: Remove?
        }
        room.SendObjects(session);
        if (habbo?.Messenger != null)
            habbo.Messenger.NotifyChangesToFriends();
            
        if (habbo?.HabboStats != null && habbo.HabboStats.QuestId > 0)
            await _questService.QuestReminder(session, habbo.HabboStats.QuestId);
            
        session.Send(new RoomEntryInfoComposer(room.RoomId, room.CheckRights(session, true)));
        session.Send(new RoomVisualizationSettingsComposer(room.WallThickness, room.FloorThickness, Convert.ToBoolean(room.Hidewall)));
        var user = habbo == null ? null : room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Username);
        if (user != null && habbo?.PetId == 0) room.SendPacket(new UserChangeComposer(user, false));
        session.Send(new RoomEventComposer(room, room.Promotion));
        if (habbo != null)
            room.GetWired()?.TriggerEvent(WiredBoxType.TriggerRoomEnter, habbo);
        if (habbo != null && UnixTimestamp.GetNow() < habbo.FloodTime && habbo.FloodTime != 0)
            session.Send(new FloodControlComposer((int)habbo.FloodTime - (int)UnixTimestamp.GetNow()));
    }
}
