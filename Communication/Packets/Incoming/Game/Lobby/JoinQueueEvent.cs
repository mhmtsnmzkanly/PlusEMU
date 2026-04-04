using System.Text;
using Plus.Communication.Packets.Outgoing.Game;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Games;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Incoming.Game.Lobby;

internal class JoinQueueEvent : IPacketEvent
{
    private readonly IGameDataManager _gameDataManager;
    private readonly IGuideService _guideService;

    public JoinQueueEvent(IGameDataManager gameDataManager, IGuideService guideService)
    {
        _gameDataManager = gameDataManager;
        _guideService = guideService;
    }
    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        var gameId = packet.ReadInt();
        if (_gameDataManager.TryGetGame(gameId, out var gameData) && gameData != null)
        {
            var ssoTicket = $"HABBOON-Fastfood-{GenerateSso(32)}-{habbo.Id}";
            session.Send(new JoinQueueComposer(gameData.Id));
            session.Send(new LoadGameComposer(gameData, ssoTicket));
            await _guideService.SetPlaying(session, true);
        }
    }

    private string GenerateSso(int length)
    {
        var characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var result = new StringBuilder(length);
        for (var i = 0; i < length; i++) result.Append(characters[Random.Shared.Next(characters.Length)]);
        return result.ToString();
    }
}
