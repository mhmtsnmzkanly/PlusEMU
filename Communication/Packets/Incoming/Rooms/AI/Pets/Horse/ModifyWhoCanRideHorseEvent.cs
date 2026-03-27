using Dapper;
using Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Pets.Horse;

internal class ModifyWhoCanRideHorseEvent : RoomPacketEvent
{
    private readonly IDatabase _database;

    public ModifyWhoCanRideHorseEvent(IDatabase database)
    {
        _database = database;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var petId = packet.ReadInt();
        if (!room.GetRoomUserManager().TryGetPet(petId, out var pet))
            return Task.CompletedTask;
        pet.PetData.AnyoneCanRide = pet.PetData.AnyoneCanRide == 1 ? 0 : 1;
        using var db = _database.Connection();
        db.Execute("UPDATE `bots_petdata` SET `anyone_ride` = @ride WHERE `id` = @id LIMIT 1",
            new { ride = pet.PetData.AnyoneCanRide, id = petId });
        room.SendPacket(new PetInformationComposer(pet.PetData));
        return Task.CompletedTask;
    }
}