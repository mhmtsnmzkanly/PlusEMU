using Plus.HabboHotel.Rooms.Games.Teams;

namespace Plus.HabboHotel.Items.Wired;

internal static class WiredTeamParser
{
    public static bool TryParseTeam(string stringData, out Team team)
    {
        team = Team.None;

        return int.TryParse(stringData, out var value) && TryParseTeam(value, out team);
    }

    public static bool TryParseTeam(int value, out Team team)
    {
        team = value switch
        {
            1 => Team.Red,
            2 => Team.Green,
            3 => Team.Blue,
            4 => Team.Yellow,
            _ => Team.None
        };

        return team != Team.None;
    }

    public static int GetEffectId(Team team) => (int)team + 39;
}
