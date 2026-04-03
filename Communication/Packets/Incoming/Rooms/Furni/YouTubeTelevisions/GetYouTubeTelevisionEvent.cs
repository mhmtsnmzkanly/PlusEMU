using Plus.Communication.Packets.Outgoing.Rooms.Furni.YouTubeTelevisions;
using Plus.Core.Language;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items.Televisions;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni.YouTubeTelevisions;

internal class GetYouTubeTelevisionEvent : IPacketEvent
{
    private readonly ITelevisionManager _televisionManager;
    private readonly ILanguageManager _languageManager;

    public GetYouTubeTelevisionEvent(ITelevisionManager televisionManager, ILanguageManager languageManager)
    {
        _televisionManager = televisionManager;
        _languageManager = languageManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo || !habbo.TryGetCurrentRoom(out _))
            return Task.CompletedTask;
        var itemId = packet.ReadInt();
        var videos = _televisionManager.TelevisionList;
        if (videos.Count == 0)
        {
            session.SendNotification(_languageManager.Require("youtube.videos.empty"));
            return Task.CompletedTask;
        }
        var dict = _televisionManager.Televisions;
        foreach (var value in RandomValues(dict).Take(1)) session.Send(new GetYouTubeVideoComposer(itemId, value.YouTubeId));
        session.Send(new GetYouTubePlaylistComposer(itemId, videos));
        return Task.CompletedTask;
    }

    private static IEnumerable<TValue> RandomValues<TKey, TValue>(IDictionary<TKey, TValue> dict)
    {
        var values = dict.Values.ToList();
        var size = dict.Count;
        while (true) yield return values[Random.Shared.Next(size)];
    }
}
