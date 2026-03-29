using Dapper;
using System.Collections.Concurrent;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Items.Wired.Boxes;
using Plus.HabboHotel.Items.Wired.Boxes.Conditions;
using Plus.HabboHotel.Items.Wired.Boxes.Effects;
using Plus.HabboHotel.Items.Wired.Boxes.Triggers;
using Plus.HabboHotel.Rooms.Chat.Commands;
using Plus.HabboHotel.Users;
using Microsoft.Extensions.Logging;

namespace Plus.HabboHotel.Rooms.Instance;

public class WiredComponent
{
    private const int MaxQueuedExecutionsPerCycle = 64;
    private static readonly Action<ILogger, uint, string, int, string, int, Exception?> TriggerQueuedLog =
        LoggerMessage.Define<uint, string, int, string, int>(
            LogLevel.Debug,
            new EventId(2001, nameof(TriggerQueuedLog)),
            "Room {RoomId} queued Wired trigger {TriggerType} for {TargetCount} target(s) with {ContextType}; queue length is {QueueLength}");
    private static readonly Action<ILogger, uint, int, int, Exception?> QueueProcessedLog =
        LoggerMessage.Define<uint, int, int>(
            LogLevel.Debug,
            new EventId(2002, nameof(QueueProcessedLog)),
            "Room {RoomId} processed {ProcessedCount} Wired queued execution(s); {RemainingCount} remain");
    private static readonly Action<ILogger, uint, int, int, Exception?> QueueSaturatedLog =
        LoggerMessage.Define<uint, int, int>(
            LogLevel.Warning,
            new EventId(2003, nameof(QueueSaturatedLog)),
            "Room {RoomId} hit the Wired per-cycle queue cap of {ProcessedCount}; {RemainingCount} queued execution(s) remain");
    private static readonly Action<ILogger, uint, double, Exception?> SlowCycleLog =
        LoggerMessage.Define<uint, double>(
            LogLevel.Warning,
            new EventId(2004, nameof(SlowCycleLog)),
            "Room {RoomId} Wired cycle took {ElapsedMs}ms to execute");

    private readonly Room _room;
    private readonly ILogger<WiredComponent> _logger;
    private readonly ConcurrentDictionary<uint, IWiredItem> _wiredItems;
    private readonly ConcurrentQueue<WiredExecutionData> _executionQueue;

    public WiredComponent(Room instance, ILogger<WiredComponent> logger) //, RoomItem Items)
    {
        _room = instance;
        _logger = logger;
        _wiredItems = new();
        _executionQueue = new();
    }

    public void OnCycle()
    {
        var start = DateTime.Now;
        ProcessExecutionQueue();
        foreach (var item in _wiredItems.ToList())
        {
            var selectedItem = _room.GetRoomItemHandler().GetItem(item.Value.Item.Id);
            if (selectedItem == null)
                TryRemove(item.Key);
            if (item.Value is IWiredCycle)
            {
                var cycle = (IWiredCycle)item.Value;
                if (cycle.TickCount <= 0)
                    cycle.OnCycle();
                else
                    cycle.TickCount--;
            }
        }
        var span = DateTime.Now - start;
        if (span.TotalMilliseconds > 400)
        {
            SlowCycleLog(_logger, _room.Id, span.TotalMilliseconds, null);
        }
    }

    public IWiredItem LoadWiredBox(Item item)
    {
        var newBox = GenerateNewBox(item);
        using var db = _room.GetDatabase().Connection();
        dynamic? row = db.QueryFirstOrDefault(
            "SELECT `string`, `bool`, `items`, `delay` FROM `wired_items` WHERE `id` = @id LIMIT 1",
            new { id = item.Id });
        if (row != null)
        {
            if (string.IsNullOrEmpty((string?)row.@string))
            {
                if (newBox.Type == WiredBoxType.ConditionMatchStateAndPosition || newBox.Type == WiredBoxType.ConditionDontMatchStateAndPosition)
                    newBox.StringData = "0;0;0";
                else if (newBox.Type == WiredBoxType.ConditionUserCountInRoom || newBox.Type == WiredBoxType.ConditionUserCountDoesntInRoom)
                    newBox.StringData = "0;0";
                else if (newBox.Type == WiredBoxType.ConditionFurniHasNoFurni)
                    newBox.StringData = "0";
                else if (newBox.Type == WiredBoxType.EffectMatchPosition)
                    newBox.StringData = "0;0;0";
                else if (newBox.Type == WiredBoxType.EffectMoveAndRotate)
                    newBox.StringData = "0;0";
            }
            newBox.StringData = ((string?)row.@string) ?? newBox.StringData;
            newBox.BoolData = (int)row.@bool == 1;
            newBox.ItemsData = ((string?)row.items) ?? string.Empty;
            if (newBox is IWiredCycle)
            {
                var box = (IWiredCycle)newBox;
                box.Delay = (int)row.delay;
            }
            foreach (var str in (((string?)row.items) ?? string.Empty).Split(';'))
            {
                var id = 0;
                var sId = "0";
                if (str.Contains(':'))
                    sId = str.Split(':')[0];
                if (int.TryParse(str, out id) || int.TryParse(sId, out id))
                {
                    var selectedItem = _room.GetRoomItemHandler().GetItem(Convert.ToUInt32(id));
                    if (selectedItem == null)
                        continue;
                    newBox.SetItems.TryAdd(selectedItem.Id, selectedItem);
                }
            }
        }
        else
        {
            newBox.ItemsData = "";
            newBox.StringData = "";
            newBox.BoolData = false;
            SaveBox(newBox);
        }
        if (!AddBox(newBox))
        {
            // ummm
        }
        return newBox;
    }

    public IWiredItem GenerateNewBox(Item item)
    {
        switch (item.Definition.WiredType)
        {
            case WiredBoxType.TriggerRoomEnter:
                return new RoomEnterBox(_room, item);
            case WiredBoxType.TriggerRepeat:
                return new RepeaterBox(_room, item);
            case WiredBoxType.TriggerStateChanges:
                return new StateChangesBox(_room, item);
            case WiredBoxType.TriggerUserSays:
                return new UserSaysBox(_room, item);
            case WiredBoxType.TriggerWalkOffFurni:
                return new UserWalksOffBox(_room, item);
            case WiredBoxType.TriggerWalkOnFurni:
                return new UserWalksOnBox(_room, item);
            case WiredBoxType.TriggerGameStarts:
                return new GameStartsBox(_room, item);
            case WiredBoxType.TriggerGameEnds:
                return new GameEndsBox(_room, item);
            case WiredBoxType.TriggerUserFurniCollision:
                return new UserFurniCollision(_room, item);
            case WiredBoxType.TriggerUserSaysCommand:
                return new UserSaysCommandBox(_room, item);
            case WiredBoxType.EffectShowMessage:
                return new ShowMessageBox(_room, item);
            case WiredBoxType.EffectTeleportToFurni:
                return new TeleportUserBox(_room, item);
            case WiredBoxType.EffectToggleFurniState:
                return new ToggleFurniBox(_room, item);
            case WiredBoxType.EffectMoveAndRotate:
                return new MoveAndRotateBox(_room, item);
            case WiredBoxType.EffectKickUser:
                return new KickUserBox(_room, item);
            case WiredBoxType.EffectMuteTriggerer:
                return new MuteTriggererBox(_room, item);
            case WiredBoxType.EffectGiveReward:
                return new GiveRewardBox(_room, item);
            case WiredBoxType.EffectMatchPosition:
                return new MatchPositionBox(_room, item);
            case WiredBoxType.EffectAddActorToTeam:
                return new AddActorToTeamBox(_room, item);
            case WiredBoxType.EffectRemoveActorFromTeam:
                return new RemoveActorFromTeamBox(_room, item);
            case WiredBoxType.ConditionFurniHasUsers:
                return new FurniHasUsersBox(_room, item);
            case WiredBoxType.ConditionTriggererOnFurni:
                return new TriggererOnFurniBox(_room, item);
            case WiredBoxType.ConditionTriggererNotOnFurni:
                return new TriggererNotOnFurniBox(_room, item);
            case WiredBoxType.ConditionFurniHasNoUsers:
                return new FurniHasNoUsersBox(_room, item);
            case WiredBoxType.ConditionFurniHasFurni:
                return new FurniHasFurniBox(_room, item);
            case WiredBoxType.ConditionIsGroupMember:
                return new IsGroupMemberBox(_room, item);
            case WiredBoxType.ConditionIsNotGroupMember:
                return new IsNotGroupMemberBox(_room, item);
            case WiredBoxType.ConditionUserCountInRoom:
                return new UserCountInRoomBox(_room, item);
            case WiredBoxType.ConditionUserCountDoesntInRoom:
                return new UserCountDoesntInRoomBox(_room, item);
            case WiredBoxType.ConditionIsWearingFx:
                return new IsWearingFxBox(_room, item);
            case WiredBoxType.ConditionIsNotWearingFx:
                return new IsNotWearingFxBox(_room, item);
            case WiredBoxType.ConditionIsWearingBadge:
                return new IsWearingBadgeBox(_room, item);
            case WiredBoxType.ConditionIsNotWearingBadge:
                return new IsNotWearingBadgeBox(_room, item);
            case WiredBoxType.ConditionMatchStateAndPosition:
                return new FurniMatchStateAndPositionBox(_room, item);
            case WiredBoxType.ConditionDontMatchStateAndPosition:
                return new FurniDoesntMatchStateAndPositionBox(_room, item);
            case WiredBoxType.ConditionFurniHasNoFurni:
                return new FurniHasNoFurniBox(_room, item);
            case WiredBoxType.ConditionActorHasHandItemBox:
                return new ActorHasHandItemBox(_room, item);
            case WiredBoxType.ConditionActorIsInTeamBox:
                return new ActorIsInTeamBox(_room, item);
            case WiredBoxType.AddonRandomEffect:
                return new AddonRandomEffectBox(_room, item);
            case WiredBoxType.EffectMoveFurniToNearestUser:
                return new MoveFurniToUserBox(_room, item);
            case WiredBoxType.EffectExecuteWiredStacks:
                return new ExecuteWiredStacksBox(_room, item);
            case WiredBoxType.EffectTeleportBotToFurniBox:
                return new TeleportBotToFurniBox(_room, item);
            case WiredBoxType.EffectBotChangesClothesBox:
                return new BotChangesClothesBox(_room, item);
            case WiredBoxType.EffectBotMovesToFurniBox:
                return new BotMovesToFurniBox(_room, item);
            case WiredBoxType.EffectBotCommunicatesToAllBox:
                return new BotCommunicatesToAllBox(_room, item);
            case WiredBoxType.EffectBotGivesHanditemBox:
                return new BotGivesHandItemBox(_room, item);
            case WiredBoxType.EffectBotFollowsUserBox:
                return new BotFollowsUserBox(_room, item);
            case WiredBoxType.EffectSetRollerSpeed:
                return new SetRollerSpeedBox(_room, item);
            case WiredBoxType.EffectRegenerateMaps:
                return new RegenerateMapsBox(_room, item);
            case WiredBoxType.EffectGiveUserBadge:
                return new GiveUserBadgeBox(_room, item);
        }
        return null!;
    }

    public bool IsTrigger(Item item) => item.Definition.IsWiredTrigger;

    public bool IsEffect(Item item) => item.Definition.IsWiredEffect;

    public bool IsCondition(Item item) => item.Definition.IsWiredCondition;

    public bool OtherBoxHasItem(IWiredItem box, uint itemId)
    {
        if (box == null)
            return false;
        ICollection<IWiredItem> items = GetEffects(box).Where(x => x.Item.Id != box.Item.Id).ToList();
        if (items != null && items.Count > 0)
        {
            foreach (var item in items)
            {
                if (item.Type != WiredBoxType.EffectMoveAndRotate && item.Type != WiredBoxType.EffectMoveFurniFromNearestUser && item.Type != WiredBoxType.EffectMoveFurniToNearestUser)
                    continue;
                if (item.SetItems == null || item.SetItems.Count == 0)
                    continue;
                if (item.SetItems.ContainsKey(itemId))
                    return true;
            }
        }
        return false;
    }

    public bool TriggerEvent(WiredBoxType type, params object[] @params)
    {
        try
        {
            if (type == WiredBoxType.TriggerUserSays)
                return QueueMatchingUserSaysTriggers(@params);

            if (type == WiredBoxType.TriggerUserSaysCommand)
                return QueueMatchingUserSaysCommandTriggers(@params);

            if (TryQueueActorItemTrigger(type, @params))
                return true;

            if (TryQueueActorTrigger(type, @params))
                return true;

            if (TryQueueParameterlessTrigger(type))
                return true;

            return false;
        }
        catch
        {
            //log.Error("Error when triggering Wired Event: " + e);
            return false;
        }
    }

    private bool TryQueueActorTrigger(WiredBoxType type, object[] @params)
    {
        if (type != WiredBoxType.TriggerRoomEnter)
            return false;

        if (@params.Length == 0 || @params[0] is not Habbo actor)
            return false;

        return EnqueueTrigger(type, null, new WiredActorTriggerContext(actor));
    }

    private bool TryQueueActorItemTrigger(WiredBoxType type, object[] @params)
    {
        if (type != WiredBoxType.TriggerWalkOnFurni &&
            type != WiredBoxType.TriggerWalkOffFurni &&
            type != WiredBoxType.TriggerUserFurniCollision &&
            type != WiredBoxType.TriggerStateChanges)
            return false;

        if (@params.Length < 2 || @params[0] is not Habbo actor || @params[1] is not Item item)
            return false;

        return EnqueueTrigger(type, null, new WiredActorItemTriggerContext(actor, item));
    }

    private bool TryQueueParameterlessTrigger(WiredBoxType type)
    {
        if (type != WiredBoxType.TriggerGameStarts && type != WiredBoxType.TriggerGameEnds)
            return false;

        return EnqueueTrigger(type);
    }

    private bool QueueMatchingUserSaysTriggers(object[] @params)
    {
        if (@params.Length < 2 || @params[0] is not Habbo actor)
            return false;

        var message = Convert.ToString(@params[1]);
        if (string.IsNullOrEmpty(message))
            return false;

        var targetIds = _wiredItems.Values
            .Where(box => box != null && box.Type == WiredBoxType.TriggerUserSays && MatchesUserSaysTrigger(box, message))
            .Select(box => box.Item.Id)
            .ToArray();

        return EnqueueTrigger(
            WiredBoxType.TriggerUserSays,
            targetIds,
            new WiredChatTriggerContext(actor, message));
    }

    private bool QueueMatchingUserSaysCommandTriggers(object[] @params)
    {
        if (@params.Length < 2 || @params[0] is not Habbo actor || @params[1] is not CommandManager commandManager)
            return false;

        var targetIds = _wiredItems.Values
            .Where(box => box != null && box.Type == WiredBoxType.TriggerUserSaysCommand && MatchesUserSaysCommandTrigger(box, commandManager))
            .Select(box => box.Item.Id)
            .ToArray();

        return EnqueueTrigger(
            WiredBoxType.TriggerUserSaysCommand,
            targetIds,
            new WiredChatTriggerContext(actor, commandManager: commandManager));
    }

    private bool EnqueueTrigger(WiredBoxType type, IReadOnlyCollection<uint>? targetItemIds = null, object? context = null)
    {
        if (!GetTriggerBoxes(type).Any())
            return false;

        if (targetItemIds != null && targetItemIds.Count == 0)
            return false;

        _executionQueue.Enqueue(new(type, targetItemIds, context));
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            TriggerQueuedLog(
                _logger,
                _room.Id,
                type.ToString(),
                targetItemIds?.Count ?? GetTriggerBoxes(type).Count(),
                context?.GetType().Name ?? nameof(WiredEmptyExecutionContext),
                _executionQueue.Count,
                null);
        }
        return true;
    }

    private IEnumerable<IWiredItem> GetTriggerBoxes(WiredBoxType type) =>
        _wiredItems.Values.Where(box => box != null && box.Type == type && IsTrigger(box.Item));

    private IEnumerable<IWiredItem> GetBoxesAt(IWiredItem item, Func<Item, bool> predicate) =>
        _wiredItems.Values.Where(box =>
            box != null &&
            predicate(box.Item) &&
            box.Item.GetX == item.Item.GetX &&
            box.Item.GetY == item.Item.GetY);

    private static bool MatchesUserSaysTrigger(IWiredItem box, string message) =>
        message.Contains($" {box.StringData}") || message.Contains($"{box.StringData} ") || message == box.StringData;

    private static bool MatchesUserSaysCommandTrigger(IWiredItem box, CommandManager commandManager) =>
        !string.IsNullOrWhiteSpace(box.StringData) &&
        commandManager.TryGetCommand(box.StringData.Replace(":", "").ToLower(), out _);

    private void ProcessExecutionQueue()
    {
        var executions = 0;
        while (executions < MaxQueuedExecutionsPerCycle && _executionQueue.TryDequeue(out var execution))
        {
            ExecuteQueuedTrigger(execution);
            executions++;
        }

        if (executions == 0)
            return;

        var remaining = _executionQueue.Count;
        if (_logger.IsEnabled(LogLevel.Debug))
            QueueProcessedLog(_logger, _room.Id, executions, remaining, null);

        if (remaining > 0)
            QueueSaturatedLog(_logger, _room.Id, executions, remaining, null);
    }

    private void ExecuteQueuedTrigger(WiredExecutionData execution)
    {
        foreach (var box in GetQueuedTriggerTargets(execution))
            box.ExecuteWithQueuedContext(execution.Context);
    }

    private IEnumerable<IWiredItem> GetQueuedTriggerTargets(WiredExecutionData execution)
    {
        var boxes = GetTriggerBoxes(execution.Type);
        if (execution.TargetItemIds == null || execution.TargetItemIds.Count == 0)
            return boxes.ToList();

        var targetIds = execution.TargetItemIds.ToHashSet();
        return boxes.Where(box => targetIds.Contains(box.Item.Id)).ToList();
    }

    public ICollection<IWiredItem> GetTriggers(IWiredItem item)
    {
        return GetBoxesAt(item, IsTrigger).ToList();
    }

    public ICollection<IWiredItem> GetEffects(IWiredItem item)
    {
        return GetBoxesAt(item, IsEffect).OrderBy(x => x.Item.GetZ).ToList();
    }

    public IWiredItem GetRandomEffect(ICollection<IWiredItem> effects)
    {
        return effects.OrderBy(x => Guid.NewGuid()).FirstOrDefault()!;
    }

    public bool ExecuteTriggerStack(IWiredItem trigger, Habbo actor)
    {
        foreach (var condition in GetConditions(trigger).ToList())
        {
            if (!condition.ExecuteWithActor(actor))
                return false;

            OnEvent(condition.Item);
        }

        return ExecuteTriggerEffects(trigger, effect => effect.ExecuteWithActor(actor));
    }

    public bool ExecuteTriggerEffectsForRoomUsers(IWiredItem trigger)
    {
        return ExecuteTriggerEffects(trigger, effect =>
        {
            var executed = false;
            foreach (var user in _room.GetRoomUserManager().GetRoomUsers().ToList())
            {
                var client = user?.GetClient();
                var habbo = client?.GetHabbo();
                if (habbo == null)
                    continue;

                if (!effect.ExecuteWithActor(habbo))
                    return false;

                executed = true;
            }

            return executed;
        });
    }

    public bool ExecuteRepeaterConditions(IWiredItem trigger)
    {
        foreach (var condition in GetConditions(trigger).ToList())
        {
            var matched = false;
            foreach (var avatar in _room.GetRoomUserManager().GetRoomUsers().ToList())
            {
                var client = avatar?.GetClient();
                var habbo = client?.GetHabbo();
                if (habbo == null)
                    continue;

                if (!condition.ExecuteWithActor(habbo))
                    continue;

                matched = true;
            }

            if (!matched)
                return false;

            OnEvent(condition.Item);
        }

        return true;
    }

    public bool ExecuteNestedStackEffects(IWiredItem trigger, Habbo actor)
    {
        foreach (var item in trigger.SetItems.Values.ToList())
        {
            if (item == null || !_room.GetRoomItemHandler().GetFloor.Contains(item) || !item.IsWired)
                continue;

            if (!TryGet(item.Id, out var wiredItem) || wiredItem.Type == WiredBoxType.EffectExecuteWiredStacks)
                continue;

            foreach (var effectItem in GetEffects(wiredItem).ToList())
            {
                if (trigger.SetItems.ContainsKey(effectItem.Item.Id) && effectItem.Item.Id != item.Id)
                    continue;
                if (effectItem.Type == WiredBoxType.EffectExecuteWiredStacks)
                    continue;
                if (!effectItem.ExecuteWithActor(actor))
                    return false;
            }
        }

        return true;
    }

    private bool ExecuteTriggerEffects(IWiredItem trigger, Func<IWiredItem, bool> effectExecutor)
    {
        var effects = GetEffects(trigger).ToList();

        var hasRandomEffectAddon = effects.Any(x => x.Type == WiredBoxType.AddonRandomEffect);
        if (hasRandomEffectAddon)
        {
            var randomBox = effects.FirstOrDefault(x => x.Type == WiredBoxType.AddonRandomEffect);
            if (randomBox == null || !randomBox.ExecuteWithoutContext())
                return false;

            var selectedBox = GetRandomEffect(effects);
            if (selectedBox == null || !selectedBox.ExecuteWithoutContext())
                return false;

            OnEvent(randomBox.Item);
            OnEvent(selectedBox.Item);
            return true;
        }

        foreach (var effect in effects)
        {
            if (effect == null || !effectExecutor(effect))
                return false;

            OnEvent(effect.Item);
        }

        return true;
    }

    public bool OnUserFurniCollision(Room room, Item item)
    {
        if (room == null || item == null)
            return false;
        foreach (var point in item.GetSides())
        {
            if (room.GetGameMap().SquareHasUsers(point.X, point.Y))
            {
                var users = room.GetGameMap().GetRoomUsers(point);
                if (users != null && users.Count > 0)
                {
                    foreach (var user in users.ToList())
                    {
                        if (user == null)
                            continue;
                        item.UserFurniCollision(user);
                    }
                }
                else
                    continue;
            }
            else
                continue;
        }
        return true;
    }

    public ICollection<IWiredItem> GetConditions(IWiredItem item)
    {
        var items = new List<IWiredItem>();
        foreach (var I in _wiredItems.Values)
        {
            if (IsCondition(I.Item) && I.Item.GetX == item.Item.GetX && I.Item.GetY == item.Item.GetY)
                items.Add(I);
        }
        return items;
    }

    public void OnEvent(Item item)
    {
        if (item.LegacyDataString == "1")
            return;
        item.LegacyDataString = "1";
        item.UpdateState(false, true);
        item.RequestUpdate(2, true);
    }

    public void SaveBox(IWiredItem item)
    {
        var items = "";
        IWiredCycle? cycle = item as IWiredCycle;
        foreach (var I in item.SetItems.Values)
        {
            var selectedItem = _room.GetRoomItemHandler().GetItem(Convert.ToUInt32(I.Id));
            if (selectedItem == null)
                continue;
            if (item.Type == WiredBoxType.EffectMatchPosition || item.Type == WiredBoxType.ConditionMatchStateAndPosition || item.Type == WiredBoxType.ConditionDontMatchStateAndPosition)
                items += $"{I.Id}:{I.GetX},{I.GetY},{I.GetZ},{I.Rotation},{I.LegacyDataString};";
            else
                items += $"{I.Id};";
        }
        if (item.Type == WiredBoxType.EffectMatchPosition || item.Type == WiredBoxType.ConditionMatchStateAndPosition || item.Type == WiredBoxType.ConditionDontMatchStateAndPosition)
            item.ItemsData = items;
        using var db = _room.GetDatabase().Connection();
        db.Execute(
            "REPLACE INTO `wired_items` VALUES (@id, @items, @delay, @string, @bool)",
            new { id = item.Item.Id, items, delay = cycle?.Delay ?? 0, @string = item.StringData, @bool = item.BoolData ? "1" : "0" });
    }

    public bool AddBox(IWiredItem item) => _wiredItems.TryAdd(item.Item.Id, item);

    public bool TryRemove(uint itemId)
    {
        return _wiredItems.TryRemove(itemId, out _);
    }

    public bool TryGet(uint id, out IWiredItem item)
    {
        if (_wiredItems.TryGetValue(id, out var wiredItem))
        {
            item = wiredItem;
            return true;
        }
        item = null!;
        return false;
    }

    public void Cleanup()
    {
        _wiredItems.Clear();
    }
}
