using Plus.HabboHotel.Items;
using Plus.HabboHotel.Users;
using System.Diagnostics.CodeAnalysis;

namespace Plus.HabboHotel.Rooms.Instance;

internal static class WiredContextResolver
{
    public static bool TryGetActor(object[] parameters, [NotNullWhen(true)] out Habbo? actor)
    {
        actor = parameters.Length switch
        {
            1 when parameters[0] is WiredActorTriggerContext actorContext => actorContext.Actor,
            1 when parameters[0] is WiredActorItemTriggerContext actorItemContext => actorItemContext.Actor,
            1 when parameters[0] is WiredChatTriggerContext chatContext => chatContext.Actor,
            > 0 => parameters[0] as Habbo,
            _ => null
        };

        return actor != null;
    }

    public static bool TryGetActorItem(object[] parameters, [NotNullWhen(true)] out Habbo? actor, [NotNullWhen(true)] out Item? item)
    {
        actor = null;
        item = null;

        if (parameters.Length == 1 && parameters[0] is WiredActorItemTriggerContext context)
        {
            actor = context.Actor;
            item = context.Item;
            return true;
        }

        if (parameters.Length < 2)
            return false;

        actor = parameters[0] as Habbo;
        item = parameters[1] as Item;
        return actor != null && item != null;
    }

    public static bool TryGetChatMessage(object[] parameters, [NotNullWhen(true)] out Habbo? actor, out string message)
    {
        actor = null;
        message = string.Empty;

        if (parameters.Length == 1 && parameters[0] is WiredChatTriggerContext context)
        {
            actor = context.Actor;
            message = context.Message ?? string.Empty;
            return actor != null;
        }

        if (!TryGetActor(parameters, out actor))
            return false;

        if (parameters.Length < 2)
            return false;

        message = Convert.ToString(parameters[1]) ?? string.Empty;
        return true;
    }
}
