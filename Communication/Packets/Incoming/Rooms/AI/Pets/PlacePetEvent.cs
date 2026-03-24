using Microsoft.Extensions.Logging;
using Plus.Communication.Packets.Outgoing.Inventory.Pets;
using Plus.Communication.Packets.Outgoing.Rooms.Notifications;
using Plus.Core.Settings;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.AI.Speech;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Pets;

internal class PlacePetEvent : RoomPacketEvent
{
    private readonly ILogger<PlacePetEvent> _logger;
    private readonly IRoomManager _roomManager;
    private readonly ISettingsManager _settingsManager;

    public PlacePetEvent(IRoomManager roomManager, ISettingsManager settingsManager, ILogger<PlacePetEvent> logger)
    {
        _roomManager = roomManager;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var petInventory = habbo?.Inventory?.Pets;
        if (petInventory == null)
            return Task.CompletedTask;

        if (room.AllowPets == false && !room.CheckRights(session, true) || !room.CheckRights(session, true))
        {
            session.Send(new RoomErrorNotifComposer(1));
            return Task.CompletedTask;
        }
        if (room.GetRoomUserManager().PetCount > Convert.ToInt32(_settingsManager.TryGetValue("room.pets.placement_limit")))
        {
            session.Send(new RoomErrorNotifComposer(2)); //5 = I have too many.
            return Task.CompletedTask;
        }
        if (!petInventory.Pets.TryGetValue(packet.ReadInt(), out var pet))
            return Task.CompletedTask;
        if (pet.PlacedInRoom)
        {
            session.SendNotification("This pet is already in the room?");
            return Task.CompletedTask;
        }
        var x = packet.ReadInt();
        var y = packet.ReadInt();
        if (!room.GetGameMap().CanWalk(x, y, false))
        {
            session.Send(new RoomErrorNotifComposer(4));
            return Task.CompletedTask;
        }
        if (room.GetRoomUserManager().TryGetPet(pet.PetId, out var oldPet)) room.GetRoomUserManager().RemoveBot(oldPet.VirtualId, false);
        pet.X = x;
        pet.Y = y;
        pet.PlacedInRoom = true;
        pet.RoomId = room.RoomId;
        var rndSpeechList = new List<RandomSpeech>();
        var roomBot = new RoomBot(pet.PetId, pet.RoomId, "pet", "freeroam", pet.Name, "", pet.Look, x, y, 0, 0, 0, 0, 0, 0, ref rndSpeechList, "", 0, pet.OwnerId, false, 0, false, 0);
        room.GetRoomUserManager().DeployBot(roomBot, pet);
        pet.DbState = PetDatabaseUpdateState.NeedsUpdate;
        room.GetRoomUserManager().UpdatePets();
        petInventory.RemovePet(pet.PetId);
        session.Send(new PetInventoryComposer(petInventory.Pets.Values.ToList()));
        return Task.CompletedTask;
    }
}
