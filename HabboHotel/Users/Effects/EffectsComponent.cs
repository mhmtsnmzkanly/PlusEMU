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

    public ICollection<AvatarEffect> GetAllEffects => _effects.Values;

    public int CurrentEffect { get; set; }

    /// <summary>
    /// Initializes the EffectsComponent.
    /// </summary>
    public bool Init(Habbo habbo)
    {
        if (_effects.Count > 0)
            return false;
        using (var connection = PlusEnvironment.DatabaseManager.Connection())
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
        CurrentEffect = 0;
        return true;
    }

    public bool TryAdd(AvatarEffect effect) => _effects.TryAdd(effect.Id, effect);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="spriteId"></param>
    /// <param name="activatedOnly"></param>
    /// <param name="unactivatedOnly"></param>
    /// <returns></returns>
    public bool HasEffect(int spriteId, bool activatedOnly = false, bool unactivatedOnly = false) => GetEffectNullable(spriteId, activatedOnly, unactivatedOnly) != null;

    /// <summary>
    /// 
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
    /// 
    /// </summary>
    /// <param name="habbo"></param>
    public void CheckEffectExpiry(Habbo habbo)
    {
        foreach (var effect in _effects.Values.ToList())
            if (effect.HasExpired)
                effect.HandleExpiration(habbo);
    }

    public void ApplyEffect(int effectId)
    {
        if (_habbo == null || _habbo.CurrentRoom == null)
            return;
        var user = _habbo.CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(_habbo.Id);
        if (user == null)
            return;
        CurrentEffect = effectId;
        if (user.IsDancing)
            _habbo.CurrentRoom.SendPacket(new DanceComposer(user, 0));
        _habbo.CurrentRoom.SendPacket(new AvatarEffectComposer(user.VirtualId, effectId));
    }

    /// <summary>
    /// Disposes the EffectsComponent.
    /// </summary>
    public void Dispose()
    {
        _effects.Clear();
    }
}
