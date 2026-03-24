using Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Pets;

internal class GetPetInformationEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || !habbo.InRoom)
            return Task.CompletedTask;
        var currentRoom = habbo.CurrentRoom;
        if (currentRoom == null)
            return Task.CompletedTask;

        var petId = packet.ReadInt();
        if (!currentRoom.GetRoomUserManager().TryGetPet(petId, out var pet))
        {
            //Okay so, we've established we have no pets in this room by this virtual Id, let us check out users, maybe they're creeping as a pet?!
            var user = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(petId);
            if (user == null)
                return Task.CompletedTask;

            //Check some values first, please!
            var userClient = user.GetClient();
            var userHabbo = userClient?.GetHabbo();
            if (userHabbo == null)
                return Task.CompletedTask;

            //And boom! Let us send the information composer 8-).
            session.Send(new PetInformationComposer(userHabbo));
            return Task.CompletedTask;
        }

        //Continue as a regular pet..
        if (pet.RoomId != currentRoom.RoomId || pet.PetData == null)
            return Task.CompletedTask;
        session.Send(new PetInformationComposer(pet.PetData));
        return Task.CompletedTask;
    }
}
