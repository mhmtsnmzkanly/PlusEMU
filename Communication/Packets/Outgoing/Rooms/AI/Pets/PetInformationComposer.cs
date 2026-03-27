using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Users;
using Plus.Utilities;

namespace Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;

public class PetInformationComposer : IServerPacket
{
    private readonly Habbo? _habbo;
    private readonly Pet? _pet;
    private readonly IRoomManager _roomManager;

    public uint MessageId => ServerPacketHeader.PetInformationComposer;

    public PetInformationComposer(Pet pet, IRoomManager roomManager)
    {
        _pet = pet;
        _roomManager = roomManager;
    }

    public PetInformationComposer(Habbo habbo, IRoomManager roomManager)
    {
        _habbo = habbo;
        _roomManager = roomManager;
    }

    public void Compose(IOutgoingPacket packet)
    {
        if (_pet != null)
        {
            if (!_roomManager.TryGetRoom(_pet.RoomId, out var room))
                return;
            packet.WriteInteger(_pet.PetId);
            packet.WriteString(_pet.Name);
            packet.WriteInteger(_pet.Level);
            packet.WriteInteger(Pet.MaxLevel);
            packet.WriteInteger(_pet.Experience);
            packet.WriteInteger(_pet.ExperienceGoal);
            packet.WriteInteger(_pet.Energy);
            packet.WriteInteger(Pet.MaxEnergy);
            packet.WriteInteger(_pet.Nutrition);
            packet.WriteInteger(Pet.MaxNutrition);
            packet.WriteInteger(_pet.Respect);
            packet.WriteInteger(_pet.OwnerId);
            packet.WriteInteger(_pet.Age);
            packet.WriteString(_pet.OwnerName);
            packet.WriteInteger(1); //3 on hab
            packet.WriteBoolean(_pet.Saddle > 0);
            packet.WriteBoolean(false);
            packet.WriteInteger(0); //5 on hab
            packet.WriteInteger(_pet.AnyoneCanRide); // Anyone can ride horse
            packet.WriteInteger(0);
            packet.WriteInteger(0); //512 on hab
            packet.WriteInteger(0); //1536
            packet.WriteInteger(0); //2560
            packet.WriteInteger(0); //3584
            packet.WriteInteger(0);
            packet.WriteString("");
            packet.WriteBoolean(false);
            packet.WriteInteger(-1); //255 on hab
            packet.WriteInteger(-1);
            packet.WriteInteger(-1);
            packet.WriteBoolean(false);
        }
        else if (_habbo != null)
        {
            packet.WriteInteger(_habbo.Id);
            packet.WriteString(_habbo.Username);
            packet.WriteInteger(_habbo.Rank);
            packet.WriteInteger(10);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(100);
            packet.WriteInteger(100);
            packet.WriteInteger(100);
            packet.WriteInteger(100);
            packet.WriteInteger(_habbo.HabboStats.Respect);
            packet.WriteInteger(_habbo.Id);
            packet.WriteInteger(Convert.ToInt32(Math.Floor((UnixTimestamp.GetNow() - _habbo.AccountCreated) / 86400))); //How?
            packet.WriteString(_habbo.Username);
            packet.WriteInteger(1); //3 on hab
            packet.WriteBoolean(false);
            packet.WriteBoolean(false);
            packet.WriteInteger(0); //5 on hab
            packet.WriteInteger(0); // Anyone can ride horse
            packet.WriteInteger(0);
            packet.WriteInteger(0); //512 on hab
            packet.WriteInteger(0); //1536
            packet.WriteInteger(0); //2560
            packet.WriteInteger(0); //3584
            packet.WriteInteger(0);
            packet.WriteString("");
            packet.WriteBoolean(false);
            packet.WriteInteger(-1); //255 on hab
            packet.WriteInteger(-1);
            packet.WriteInteger(-1);
            packet.WriteBoolean(false);
        }
    }
}