using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;

namespace Plus.Communication.Packets.Outgoing.Catalog;

public class GroupFurniConfigComposer : IServerPacket
{
    private readonly ICollection<Group> _groups;
    private readonly IGroupManager _groupManager;
    public uint MessageId => ServerPacketHeader.GroupFurniConfigComposer;

    public GroupFurniConfigComposer(ICollection<Group> groups, IGroupManager groupManager)
    {
        _groups = groups;
        _groupManager = groupManager;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_groups.Count);
        foreach (var group in _groups)
        {
            packet.WriteInteger(group.Id);
            packet.WriteString(group.Name);
            packet.WriteString(group.Badge);
            packet.WriteString(_groupManager.GetColourCode(group.Colour1, true)); // TODO @80O: Pass by constructor / attach to Group object
            packet.WriteString(_groupManager.GetColourCode(group.Colour2, false)); // TODO @80O: Pass by constructor / attach to Group object
            packet.WriteBoolean(false);
            packet.WriteInteger(group.CreatorId);
            packet.WriteBoolean(group.ForumEnabled);
        }
    }
}