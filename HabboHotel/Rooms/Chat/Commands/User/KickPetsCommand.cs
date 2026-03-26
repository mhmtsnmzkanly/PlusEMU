using Plus.Communication.Packets.Outgoing.Inventory.Pets;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Dapper;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class KickPetsCommand : IChatCommand
{
    private readonly IGameClientManager _gameClientManager;
    private readonly IDatabase _database;
    public string Key => "kickpets";
    public string PermissionRequired => "command_kickpets";

    public string Parameters => "";

    public string Description => "Kick all of the pets from the room.";

    public KickPetsCommand(IGameClientManager gameClientManager, IDatabase database)
    {
        _gameClientManager = gameClientManager;
        _database = database;
    }

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        var inventory = habbo?.Inventory?.Pets;
        if (habbo == null || inventory == null)
            return;

        if (!room.CheckRights(session, true))
        {
            session.SendWhisper("Oops, only the room owner can run this command!");
            return;
        }
        if (room.GetRoomUserManager().GetPets().Count == 0) session.SendWhisper("Oops, there isn't any pets in here!?");
        foreach (var bot in room.GetRoomUserManager().GetUserList().ToList())
        {
            if (bot == null)
                continue;
            if (bot.RidingHorse)
            {
                var rider = room.GetRoomUserManager().GetRoomUserByVirtualId(bot.HorseId);
                if (rider != null)
                {
                    rider.RidingHorse = false;
                    rider.ApplyEffect(-1);
                    rider.MoveTo(new(rider.X + 1, rider.Y + 1));
                }
                else
                    bot.RidingHorse = false;
            }
            var pet = bot.PetData;
            if (pet == null)
                continue;
            pet.RoomId = 0;
            pet.PlacedInRoom = false;
            room.GetRoomUserManager().RemoveBot(bot.VirtualId, false);
            if (pet.OwnerId != habbo.Id)
            {
                var targetClient = _gameClientManager.GetClientByUserId(pet.OwnerId);
                var targetPets = targetClient?.GetHabbo()?.Inventory?.Pets;
                if (targetClient != null && targetPets != null && targetPets.AddPet(pet))
                    targetClient.Send(new PetInventoryComposer(targetPets.Pets.Values.ToList()));
            }
            if (inventory.AddPet(pet))
                session.Send(new PetInventoryComposer(inventory.Pets.Values.ToList()));
            using var connection = _database.Connection();
            connection.Execute("UPDATE `bots` SET `room_id` = '0', `x` = '0', `Y` = '0', `Z` = '0' WHERE `id` = @id LIMIT 1", new { id = pet.PetId });
            connection.Execute(
                "UPDATE `bots_petdata` SET `experience` = @exp, `energy` = @eng, `nutrition` = @nut, `respect` = @res WHERE `id` = @id LIMIT 1", 
                new { exp = pet.Experience, eng = pet.Energy, nut = pet.Nutrition, res = pet.Respect, id = pet.PetId });
        }
        session.SendWhisper("All pets have been kicked from the room.");
    }
}
