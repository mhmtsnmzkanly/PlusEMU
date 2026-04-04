using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Campaign;

public sealed class SeasonalCalendarDataComposer : IServerPacket
{
    private readonly string _campaignName;
    private readonly string _campaignImage;
    private readonly int _currentDay;
    private readonly int _totalDays;
    private readonly IReadOnlyCollection<int> _openedBoxes;
    private readonly IReadOnlyCollection<int> _lateBoxes;

    public SeasonalCalendarDataComposer(
        string campaignName,
        string campaignImage,
        int currentDay,
        int totalDays,
        IReadOnlyCollection<int> openedBoxes,
        IReadOnlyCollection<int> lateBoxes)
    {
        _campaignName = campaignName;
        _campaignImage = campaignImage;
        _currentDay = currentDay;
        _totalDays = totalDays;
        _openedBoxes = openedBoxes;
        _lateBoxes = lateBoxes;
    }

    public uint MessageId => ServerPacketHeader.UnknownCalendarComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteString(_campaignName);
        packet.WriteString(_campaignImage);
        packet.WriteInteger(_currentDay);
        packet.WriteInteger(_totalDays);
        packet.WriteInteger(_openedBoxes.Count);
        foreach (var day in _openedBoxes.OrderBy(day => day))
            packet.WriteInteger(day);
        packet.WriteInteger(_lateBoxes.Count);
        foreach (var day in _lateBoxes.OrderBy(day => day))
            packet.WriteInteger(day);
    }
}
