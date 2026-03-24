using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Users;

namespace Plus.Communication.Packets.Outgoing.Pets;

public class RespectPetNotificationComposer : IServerPacket
{
    private readonly Habbo? _habbo;
    private readonly RoomUser? _user;
    private readonly Pet? _pet;

    public uint MessageId => ServerPacketHeader.RespectPetNotificationComposer;

    public RespectPetNotificationComposer(Pet pet)
    {
        _pet = pet;
    }

    public RespectPetNotificationComposer(Habbo habbo, RoomUser user)
    {
        _habbo = habbo;
        _user = user;
    }

    public void Compose(IOutgoingPacket packet)
    {
        if (_pet != null)
        {
            packet.WriteInteger(_pet.VirtualId);
            packet.WriteInteger(_pet.VirtualId);
            packet.WriteInteger(_pet.PetId); //Pet Id, 100%
            packet.WriteString(_pet.Name);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteString(_pet.Color);
            packet.WriteInteger(0);
            packet.WriteInteger(0); //Count - 3 ints.
            packet.WriteInteger(1);
        }
        else
        {
            if (_user == null || _habbo == null)
                return;
            packet.WriteInteger(_user.VirtualId);
            packet.WriteInteger(_user.VirtualId);
            packet.WriteInteger(_habbo.Id); //Pet Id, 100%
            packet.WriteString(_habbo.Username);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteString("FFFFFF"); //Yeah..
            packet.WriteInteger(0);
            packet.WriteInteger(0); //Count - 3 ints.
            packet.WriteInteger(1);
        }
    }
}
