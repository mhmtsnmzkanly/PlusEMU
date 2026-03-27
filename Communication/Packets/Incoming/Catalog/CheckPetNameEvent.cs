using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.HabboHotel.Catalog.Utilities;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Chat.Filter;

namespace Plus.Communication.Packets.Incoming.Catalog;

public class CheckPetNameEvent : IPacketEvent
{
    private readonly IWordFilterManager _wordFilterManager;
    private readonly IPetUtility _petUtility;

    public CheckPetNameEvent(IWordFilterManager wordFilterManager, IPetUtility petUtility)
    {
        _wordFilterManager = wordFilterManager;
        _petUtility = petUtility;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var petName = packet.ReadString();
        if (petName.Length < 2)
        {
            session.Send(new CheckPetNameComposer(2, "2"));
            return Task.CompletedTask;
        }
        if (petName.Length > 15)
        {
            session.Send(new CheckPetNameComposer(1, "15"));
            return Task.CompletedTask;
        }
        if (!_petUtility.CheckPetName(petName))
        {
            session.Send(new CheckPetNameComposer(3, string.Empty));
            return Task.CompletedTask;
        }

        if (_wordFilterManager.IsFiltered(petName))
        {
            session.Send(new CheckPetNameComposer(4, string.Empty));
            return Task.CompletedTask;
        }
        session.Send(new CheckPetNameComposer(0, string.Empty));
        return Task.CompletedTask;
    }
}