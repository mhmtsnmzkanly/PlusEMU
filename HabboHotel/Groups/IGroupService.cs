using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Groups;

public interface IGroupService
{
    Task JoinGroup(GameClient session, int groupId);
    Task AcceptMembership(GameClient session, int groupId, int userId);
    Task DeclineMembership(GameClient session, int groupId, int userId);
    Task SetFavourite(GameClient session, int groupId);
    Task RemoveFavourite(GameClient session);
    Task GiveAdminRights(GameClient session, int groupId, int userId);
    Task TakeAdminRights(GameClient session, int groupId, int userId);
    Task RemoveMember(GameClient session, int groupId, int userId);
    Task UpdateSettings(GameClient session, int groupId, int type, int furniOptions);
    Task UpdateIdentity(GameClient session, int groupId, string name, string description);
    Task UpdateBadge(GameClient session, int groupId, IReadOnlyCollection<(int baseId, int firstPart, int secondPart)> parts);
    Task UpdateColours(GameClient session, int groupId, int mainColour, int secondaryColour);
    Task DeleteGroup(GameClient session, int groupId);
    Task PurchaseGroup(GameClient session, string name, string description, uint roomId, int mainColour, int secondaryColour, IReadOnlyCollection<(int baseId, int firstPart, int secondPart)> parts);
}
