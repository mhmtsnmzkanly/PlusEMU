using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;
using Plus.Utilities;

namespace Plus.Communication.Packets.Incoming.Rooms.Engine;

internal class GetRoomEntryDataEvent : IPacketEvent
{
    private readonly IQuestService _questService;
    private readonly IRoomService _roomService;

    public GetRoomEntryDataEvent(IQuestService questService, IRoomService roomService)
    {
        _questService = questService;
        _roomService = roomService;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || !habbo.TryGetCurrentRoom(out var room))
            return;
        if (!room.GetRoomUserManager().AddAvatarToRoom(session))
        {
            await _roomService.LeaveRoom(session, false);
            return; //TODO: Remove?
        }
        room.SendObjects(session);
        if (habbo.Messenger != null)
            habbo.Messenger.NotifyChangesToFriends();
            
        if (habbo.HabboStats != null && habbo.HabboStats.QuestId > 0)
            await _questService.QuestReminder(session, habbo.HabboStats.QuestId);
            
        session.Send(new RoomEntryInfoComposer(room.RoomId, room.CheckRights(session, true)));
        session.Send(new RoomVisualizationSettingsComposer(room.WallThickness, room.FloorThickness, Convert.ToBoolean(room.Hidewall)));
        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Username);
        if (user != null && habbo.PetId == 0) room.SendPacket(new UserChangeComposer(user, false));
        session.Send(new RoomEventComposer(room, room.Promotion));
        room.GetWired()?.TriggerEvent(WiredBoxType.TriggerRoomEnter, habbo);
        if (UnixTimestamp.GetNow() < habbo.FloodTime && habbo.FloodTime != 0)
            session.Send(new FloodControlComposer((int)habbo.FloodTime - (int)UnixTimestamp.GetNow()));
    }
}
