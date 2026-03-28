using Plus.Database;
using System.Collections.Concurrent;
using Dapper;
using Plus.Communication.Packets.Outgoing.Rooms.Avatar;
using Plus.Utilities;

namespace Plus.HabboHotel.Users.Effects;

public sealed class EffectsComponent
{
    private sealed class UserEffectRow
    {
        public int Id { get; init; }
        public int UserId { get; init; }
        public int EffectId { get; init; }
        public double TotalDuration { get; init; }
        public string IsActivated { get; init; } = "0";
        public double ActivatedStamp { get; init; }
        public int Quantity { get; init; }
    }

    /// <summary>
    /// Effects stored by ID > Effect.
    /// </summary>
    private readonly ConcurrentDictionary<int, AvatarEffect> _effects = new();
    private Habbo? _habbo;
    private IDatabase? _database;

    public ICollection<AvatarEffect> GetAllEffects => _effects.Values;

    public int CurrentEffect { get; set; }

    /// <summary>
    /// Initializes the EffectsComponent.
    /// </summary>
    public bool Init(Habbo habbo, IDatabase database)
    {
        if (_effects.Count > 0)
            return false;
        using (var connection = database.Connection())
        {
            foreach (var row in connection.Query<UserEffectRow>(
                         """
                         SELECT
                             `id` AS Id,
                             `user_id` AS UserId,
                             `effect_id` AS EffectId,
                             `total_duration` AS TotalDuration,
                             `is_activated` AS IsActivated,
                             `activated_stamp` AS ActivatedStamp,
                             `quantity` AS Quantity
                         FROM `user_effects`
                         WHERE `user_id` = @id
                         """,
                         new { id = habbo.Id }))
            {
                _effects.TryAdd(row.Id,
                    new(row.Id, row.UserId, row.EffectId, row.TotalDuration,
                        ConvertExtensions.EnumToBool(row.IsActivated), row.ActivatedStamp, row.Quantity));
            }
        }
        _habbo = habbo;
        _database = database;
        CurrentEffect = 0;
        return true;
    }

    /// <summary>
    /// Creates a new AvatarEffect with the specified details.
    /// </summary>
    /// <param name="spriteId"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    public AvatarEffect? CreateEffect(int spriteId, double duration)
    {
        if (_habbo == null || _database == null)
            return null;
        using var connection = _database.Connection();
        var id = Convert.ToInt32(connection.ExecuteScalar<long>(
            "INSERT INTO `user_effects` (`user_id`,`effect_id`,`total_duration`,`is_activated`,`activated_stamp`,`quantity`) VALUES(@uid,@sid,@dur,'0',0,1); SELECT LAST_INSERT_ID();",
            new { uid = _habbo.Id, sid = spriteId, dur = duration }));
        var effect = new AvatarEffect(id, _habbo.Id, spriteId, duration, false, 0, 1);
        _effects.TryAdd(id, effect);
        return effect;
    }

    public bool TryAdd(AvatarEffect effect) => _effects.TryAdd(effect.Id, effect);

    /// <summary>
    /// Checks if the user has an effect with the specified sprite ID.
    /// </summary>
    /// <param name="spriteId"></param>
    /// <param name="activatedOnly"></param>
    /// <param name="unactivatedOnly"></param>
    /// <returns></returns>
    public bool HasEffect(int spriteId, bool activatedOnly = false, bool unactivatedOnly = false) => GetEffectNullable(spriteId, activatedOnly, unactivatedOnly) != null;

    /// <summary>
    /// Retrieves an AvatarEffect by its sprite ID, with optional filtering for activation status.
    /// </summary>
    /// <param name="spriteId"></param>
    /// <param name="activatedOnly"></param>
    /// <param name="unactivatedOnly"></param>
    /// <returns></returns>
    public AvatarEffect? GetEffectNullable(int spriteId, bool activatedOnly = false, bool unactivatedOnly = false)
    {
        foreach (var effect in _effects.Values.ToList())
            if (!effect.HasExpired && effect.SpriteId == spriteId && (!activatedOnly || effect.Activated) && (!unactivatedOnly || !effect.Activated))
                return effect;
        return null;
    }

    /// <summary>
    /// Checks effect expiration and handles it.
    /// </summary>
    /// <param name="habbo"></param>
    /// <param name="database"></param>
    public void CheckEffectExpiry(Habbo habbo, IDatabase database)
    {
        foreach (var effect in _effects.Values.ToList())
            if (effect.HasExpired)
                effect.HandleExpiration(habbo, database);
    }

    public void ApplyEffect(int effectId)
    {
        if (_habbo == null || !_habbo.TryGetCurrentRoom(out var room))
            return;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(_habbo.Id);
        if (user == null)
            return;

        CurrentEffect = effectId;
        if (user.IsDancing)
            room.SendPacket(new DanceComposer(user, 0));
        room.SendPacket(new AvatarEffectComposer(user.VirtualId, effectId));
    }

    /// <summary>
    /// Disposes the EffectsComponent.
    /// </summary>
    public void Dispose()
    {
        _effects.Clear();
    }
}
