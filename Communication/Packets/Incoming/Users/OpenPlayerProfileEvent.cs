using Plus.Communication.Packets.Outgoing.Users;
using Plus.Core.Language;
using Plus.HabboHotel.Friends;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.UserData;

namespace Plus.Communication.Packets.Incoming.Users;

internal class OpenPlayerProfileEvent : IPacketEvent
{
    private readonly IGroupManager _groupManager;
    private readonly IMessengerDataLoader _messengerDataLoader;
    private readonly IGameClientManager _gameClientManager;
    private readonly IUserDataFactory _userDataFactory;
    private readonly HabboStatsService _habboStatsService;
    private readonly ILanguageManager _languageManager;

    public OpenPlayerProfileEvent(IGroupManager groupManager, IMessengerDataLoader messengerDataLoader, IGameClientManager gameClientManager, IUserDataFactory userDataFactory, HabboStatsService habboStatsService, ILanguageManager languageManager)
    {
        _groupManager = groupManager;
        _messengerDataLoader = messengerDataLoader;
        _gameClientManager = gameClientManager;
        _userDataFactory = userDataFactory;
        _habboStatsService = habboStatsService;
        _languageManager = languageManager;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var userId = packet.ReadInt();
        packet.ReadBool(); //IsMe?

        var targetData = _gameClientManager.GetClientByUserId(userId)?.GetHabbo();

        targetData ??= await _userDataFactory.GetUserDataByIdAsync(userId);

        if (targetData == null)
        {
            session.SendNotification(_languageManager.TryGetValue("user.not_found"));
            return;
        }

        // Load HabboStats here
        var habboStats = await _habboStatsService.LoadHabboStats(userId);
        targetData.HabboStats = habboStats;

        var groups = _groupManager.GetGroupsForUser(targetData.Id);
        var friendCount = await _messengerDataLoader.GetFriendCount(userId);
        session.Send(new ProfileInformationComposer(targetData, session, groups, friendCount, habboStats, _gameClientManager, _groupManager));
    }
}
