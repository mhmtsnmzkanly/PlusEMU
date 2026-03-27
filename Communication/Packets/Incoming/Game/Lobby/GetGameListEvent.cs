using Plus.Communication.Packets.Outgoing.Game;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Games;

namespace Plus.Communication.Packets.Incoming.Game.Lobby;

internal class GetGameListEvent : IPacketEvent
{
    private readonly IGameDataManager _gameDataManager;

    public GetGameListEvent(IGameDataManager gameDataManager)
    {
        _gameDataManager = gameDataManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        session.Send(new GameListComposer(_gameDataManager.GameData, _gameDataManager));
        return Task.CompletedTask;
    }
}