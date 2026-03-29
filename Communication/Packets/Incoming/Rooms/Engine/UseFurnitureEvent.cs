using Dapper;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Communication.Packets.Outgoing.Rooms.Furni;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Engine;

internal class UseFurnitureEvent : RoomPacketEvent
{
    private readonly IQuestService _questService;
    private readonly IDatabase _database;

    public UseFurnitureEvent(IQuestService questService, IDatabase database)
    {
        _questService = questService;
        _database = database;
    }

    public override async Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var itemId = packet.ReadUInt();
        var item = room.GetRoomItemHandler().GetItem(itemId);
        if (item == null) return;
        var hasRights = room.CheckRights(session, false, true);
        if (item.Definition.InteractionType == InteractionType.Banzaitele) return;
        if (item.Definition.IsToner)
        {
            if (!room.CheckRights(session, true)) return;
            room.TonerData ??= new(item.Id, _database);
            room.TonerData.Enabled = room.TonerData.Enabled == 0 ? 1 : 0;
            room.SendPacket(new ObjectUpdateComposer(item));
            item.UpdateState();
            using var db = _database.Connection();
            db.Execute("UPDATE `room_items_toner` SET `enabled` = @enabled LIMIT 1", new { enabled = room.TonerData.Enabled });
            return;
        }
        if (item.Definition.InteractionType == InteractionType.GnomeBox && item.UserId == habbo?.Id)
            session.Send(new GnomeBoxComposer(item.Id));
        var toggle = true;
        if (item.Definition.InteractionType == InteractionType.WfFloorSwitch1 || item.Definition.InteractionType == InteractionType.WfFloorSwitch2)
        {
            var user = habbo == null ? null : item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
            if (user == null) return;
            if (!Gamemap.TilesTouching(item.GetX, item.GetY, user.X, user.Y)) toggle = false;
        }
        var request = packet.ReadInt();
        item.Interactor.OnTrigger(session, item, request, hasRights);
        if (toggle && habbo != null)
            item.GetRoom().GetWired().TriggerEvent(WiredBoxType.TriggerStateChanges, habbo, item);
        await _questService.ProgressUserQuest(session, QuestType.ExploreFindItem, (int)item.Definition.Id);
    }
}
