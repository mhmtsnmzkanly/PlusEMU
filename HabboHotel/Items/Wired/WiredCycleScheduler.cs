namespace Plus.HabboHotel.Items.Wired;

internal static class WiredCycleScheduler
{
    public static int GetTickCountForDelay(int delay, bool extraTick = false) =>
        extraTick ? delay + 1 : delay;

    public static long GetNextTicks(long nextTicks, int delay) =>
        nextTicks == 0 || nextTicks < DateTime.UtcNow.Ticks
            ? DateTime.UtcNow.Ticks + delay
            : nextTicks;

    public static bool IsReady(bool requested, long nextTicks) =>
        requested && nextTicks != 0 && nextTicks < DateTime.UtcNow.Ticks;

    public static bool ShouldMarkRequested(bool requested) => !requested;

    public static bool MarkRequested(ref bool requested)
    {
        if (requested)
            return false;

        requested = true;
        return true;
    }

    public static bool Schedule(ref long nextTicks, ref bool requested, int delay)
    {
        nextTicks = GetNextTicks(nextTicks, delay);
        return MarkRequested(ref requested);
    }

    public static void Reset(ref long nextTicks, ref bool requested)
    {
        nextTicks = 0;
        requested = false;
    }

    public static void Reset(ref bool requested)
    {
        requested = false;
    }
}
