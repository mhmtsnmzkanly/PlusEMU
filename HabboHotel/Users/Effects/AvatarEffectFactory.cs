using Dapper;

namespace Plus.HabboHotel.Users.Effects;

internal static class AvatarEffectFactory
{
    /// <summary>
    /// Creates a new AvatarEffect with the specified details.
    /// </summary>
    /// <param name="habbo"></param>
    /// <param name="spriteId"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    public static AvatarEffect CreateNullable(Habbo habbo, int spriteId, double duration, Plus.Database.IDatabase database)
    {
        using var connection = database.Connection();
        var id = Convert.ToInt32(connection.ExecuteScalar<long>(
            "INSERT INTO `user_effects` (`user_id`,`effect_id`,`total_duration`,`is_activated`,`activated_stamp`,`quantity`) VALUES(@uid,@sid,@dur,'0',0,1); SELECT LAST_INSERT_ID();",
            new { uid = habbo.Id, sid = spriteId, dur = duration }));
        return new(id, habbo.Id, spriteId, duration, false, 0, 1);
    }
}
