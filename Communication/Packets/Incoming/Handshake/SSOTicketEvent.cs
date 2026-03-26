using Microsoft.Extensions.Logging;
using Plus.Communication.Attributes;
using Plus.Communication.Packets.Outgoing.BuildersClub;
using Plus.Communication.Packets.Outgoing.Handshake;
using Plus.Communication.Packets.Outgoing.Inventory.Achievements;
using Plus.Communication.Packets.Outgoing.Inventory.AvatarEffects;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.Communication.Packets.Outgoing.Notifications;
using Plus.Communication.Packets.Outgoing.Sound;
using Plus.Core.FigureData;
using Plus.Core.Language;
using Plus.Core.Settings;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Badges;
using Plus.HabboHotel.Cache;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;
using Plus.HabboHotel.Permissions;
using Plus.HabboHotel.Rewards;
using Plus.HabboHotel.Subscriptions;
using Plus.HabboHotel.Users.Authentication;
using Plus.HabboHotel.Users.Messenger.FriendBar;

namespace Plus.Communication.Packets.Incoming.Handshake;

[NoAuthenticationRequired]
public class SsoTicketEvent : IPacketEvent
{
    private readonly IAuthenticator _authenticate;
    private readonly IBadgeManager _badgeManager;
    private readonly IModerationManager _moderationManager;
    private readonly IAchievementManager _achievementManager;
    private readonly IPermissionManager _permissionManager;
    private readonly ISubscriptionManager _subscriptionManager;
    private readonly ICacheManager _cacheManager;
    private readonly IFigureDataManager _figureManager;
    private readonly ILanguageManager _languageManager;
    private readonly ISettingsManager _settingsManager;
    private readonly IRewardManager _rewardManager;
    private readonly ILogger<SsoTicketEvent> _logger;

    public SsoTicketEvent(IAuthenticator authenticate,
        IBadgeManager badgeManager,
        IModerationManager moderationManager,
        IAchievementManager achievementManager,
        IPermissionManager permissionManager,
        ISubscriptionManager subscriptionManager,
        ICacheManager cacheManager,
        IFigureDataManager figureManager,
        ILanguageManager languageManager,
        ISettingsManager settingsManager,
        IRewardManager rewardManager,
        ILogger<SsoTicketEvent> logger)
    {
        _authenticate = authenticate;
        _badgeManager = badgeManager;
        _moderationManager = moderationManager;
        _achievementManager = achievementManager;
        _permissionManager = permissionManager;
        _subscriptionManager = subscriptionManager;
        _cacheManager = cacheManager;
        _figureManager = figureManager;
        _languageManager = languageManager;
        _settingsManager = settingsManager;
        _rewardManager = rewardManager;
        _logger = logger;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var sso = packet.ReadString() ?? string.Empty;
        _logger.LogInformation("Received SsoTicketEvent for session {sessionId}. Build: {build}. TicketLength: {ticketLength}.", session.Id, session.ClientBuild ?? "<unknown>", sso.Length);
        var error = await _authenticate.AuthenticateUsingSSO(session, sso);
        if (error != null)
        {
            _logger.LogWarning("SSO authentication failed for session {sessionId}: {error}.", session.Id, error);
            session.Disconnect($"SSO authentication failed: {error}");
            return;
        }

        if (error == null)
        {
            var habbo = session.GetHabbo();
            var effects = habbo.Effects;
            var clothing = habbo.Clothing;
            var inventory = habbo.Inventory;
            _logger.LogInformation("SSO authentication succeeded for session {sessionId}. HabboId: {habboId}, Username: {username}.", session.Id, habbo.Id, habbo.Username);
            session.Send(new AuthenticationOkComposer());

            // TODO @80O: Move to individual incoming message handlers.
            session.Send(new AvatarEffectsComposer(effects?.GetAllEffects ?? new List<Plus.HabboHotel.Users.Effects.AvatarEffect>()));
            session.Send(new NavigatorSettingsComposer(habbo.HomeRoom));
            session.Send(new FavouritesComposer(habbo.FavoriteRooms));
            session.Send(new FigureSetIdsComposer(clothing?.GetClothingParts ?? Array.Empty<Plus.HabboHotel.Users.Clothing.Parts.ClothingParts>()));
            session.Send(new UserRightsComposer(habbo.Rank, habbo.IsAmbassador));
            session.Send(new AvailabilityStatusComposer());
            session.Send(new AchievementScoreComposer(habbo.HabboStats.AchievementPoints));
            session.Send(new BuildersClubMembershipComposer());
            session.Send(new CfhTopicsInitComposer(_moderationManager.UserActionPresets));
            session.Send(new BadgeDefinitionsComposer(_achievementManager.Achievements));
            session.Send(new SoundSettingsComposer(habbo.ClientVolume, habbo.ChatPreference, habbo.AllowMessengerInvites,
                habbo.FocusPreference,
                FriendBarStateUtility.GetInt(habbo.FriendbarState)));
            //SendMessage(new TalentTrackLevelComposer());


            if (_permissionManager.TryGetGroup(habbo.Rank, out var group) && group != null)
            {
                if (!string.IsNullOrEmpty(group.Badge))
                {
                    if (inventory?.Badges != null && !inventory.Badges.HasBadge(group.Badge))
                        await _badgeManager.GiveBadge(habbo, group.Badge);
                }
            }
            if (_subscriptionManager.TryGetSubscriptionData(habbo.VipRank, out var subData) && subData != null)
            {
                if (!string.IsNullOrEmpty(subData.Badge))
                {
                    if (inventory?.Badges != null && !inventory.Badges.HasBadge(subData.Badge))
                        await _badgeManager.GiveBadge(habbo, subData.Badge);
                }
            }
            if (!_cacheManager.ContainsUser(habbo.Id))
                _cacheManager.GenerateUser(habbo.Id);
            habbo.Look = _figureManager.ProcessFigure(habbo.Look, habbo.Gender, clothing?.GetClothingParts ?? Array.Empty<Plus.HabboHotel.Users.Clothing.Parts.ClothingParts>(), true);
            habbo.InitProcess();
            if (habbo.Permissions?.HasRight("mod_tickets") == true)
            {
                session.Send(new ModeratorInitComposer(
                    _moderationManager.UserMessagePresets,
                    _moderationManager.RoomMessagePresets,
                    _moderationManager.GetTickets));
            }
            if (_settingsManager.TryGetValue("user.login.message.enabled") == "1")
                session.Send(new MotdNotificationComposer(_languageManager.TryGetValue("user.login.message")));
            await _rewardManager.CheckRewards(session);
        }
    }
}
