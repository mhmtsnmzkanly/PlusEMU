using System.Collections.Generic;

namespace Plus.HabboHotel.Users.Calendar;

public sealed class CalendarComponent
{
    private readonly List<int> _lateBoxes;
    private readonly List<int> _openedBoxes;

    public CalendarComponent(List<int> lateBoxes, List<int> openedBoxes)
    {
        _lateBoxes = lateBoxes;
        _openedBoxes = openedBoxes;
    }

    public List<int> GetOpenedBoxes() => _openedBoxes;

    public List<int> GetLateBoxes() => _lateBoxes;

    public void Dispose()
    {
        _lateBoxes.Clear();
        _openedBoxes.Clear();
    }
}
