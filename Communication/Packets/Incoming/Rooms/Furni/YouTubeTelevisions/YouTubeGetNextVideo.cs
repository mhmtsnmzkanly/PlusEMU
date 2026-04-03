using Plus.Communication.Packets.Outgoing.Rooms.Furni.YouTubeTelevisions;
using Plus.Core.Language;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items.Televisions;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni.YouTubeTelevisions;

internal class YouTubeGetNextVideo : IPacketEvent
{
    private readonly ITelevisionManager _televisionManager;
    private readonly ILanguageManager _languageManager;

    public YouTubeGetNextVideo(ITelevisionManager televisionManager, ILanguageManager languageManager)
    {
        _televisionManager = televisionManager;
        _languageManager = languageManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo || !habbo.TryGetCurrentRoom(out _))
            return Task.CompletedTask;
        var videos = _televisionManager.TelevisionList;
        if (videos.Count == 0)
        {
            session.SendNotification(_languageManager.Require("youtube.videos.empty"));
            return Task.CompletedTask;
        }
        var itemId = packet.ReadInt();
        packet.ReadInt(); //next
        TelevisionItem? item = null;
        var dict = _televisionManager.Televisions;
        foreach (var value in RandomValues(dict).Take(1)) item = value;
        if (item == null)
        {
            session.SendNotification(_languageManager.Require("youtube.video.fetch_failed"));
            return Task.CompletedTask;
        }
        session.Send(new GetYouTubeVideoComposer(itemId, item.YouTubeId));
        return Task.CompletedTask;
    }

    private static IEnumerable<TValue> RandomValues<TKey, TValue>(IDictionary<TKey, TValue> dict)
    {
        var values = dict.Values.ToList();
        var size = dict.Count;
        while (true) yield return values[Random.Shared.Next(size)];
    }
}
