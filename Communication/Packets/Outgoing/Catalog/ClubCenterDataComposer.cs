using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Catalog;

public sealed class ClubCenterDataComposer : IServerPacket
{
    private readonly int _currentHcStreak;
    private readonly string _firstSubDate;
    private readonly double _kickbackPercentage;
    private readonly int _totalCreditsMissed;
    private readonly int _totalCreditsRewarded;
    private readonly int _totalCreditsSpent;
    private readonly int _creditRewardForStreakBonus;
    private readonly int _creditRewardForMonthlySpent;
    private readonly int _timeUntilPayday;
    private readonly int _windowId;

    public ClubCenterDataComposer(
        int currentHcStreak,
        string firstSubDate,
        double kickbackPercentage,
        int totalCreditsMissed,
        int totalCreditsRewarded,
        int totalCreditsSpent,
        int creditRewardForStreakBonus,
        int creditRewardForMonthlySpent,
        int timeUntilPayday,
        int windowId)
    {
        _currentHcStreak = currentHcStreak;
        _firstSubDate = firstSubDate;
        _kickbackPercentage = kickbackPercentage;
        _totalCreditsMissed = totalCreditsMissed;
        _totalCreditsRewarded = totalCreditsRewarded;
        _totalCreditsSpent = totalCreditsSpent;
        _creditRewardForStreakBonus = creditRewardForStreakBonus;
        _creditRewardForMonthlySpent = creditRewardForMonthlySpent;
        _timeUntilPayday = timeUntilPayday;
        _windowId = windowId;
    }

    public uint MessageId => ServerPacketHeader.ClubCenterDataComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_currentHcStreak);
        packet.WriteString(_firstSubDate);
        packet.WriteDouble(_kickbackPercentage);
        packet.WriteInteger(_totalCreditsMissed);
        packet.WriteInteger(_totalCreditsRewarded);
        packet.WriteInteger(_totalCreditsSpent);
        packet.WriteInteger(_creditRewardForStreakBonus);
        packet.WriteInteger(_creditRewardForMonthlySpent);
        packet.WriteInteger(_timeUntilPayday);
        packet.WriteInteger(_windowId);
    }
}
