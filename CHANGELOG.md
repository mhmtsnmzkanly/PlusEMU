# Changelog

### [Unreleased]
#### Changed
- Continued the DI/service transition in the login runtime path by introducing `IHabboRuntimeInitializer`, centralizing habbo visual/process bootstrap, and removing the loose `Habbo.InitProcess(..., object, object)` handoff.
- Continued the DI/service transition around late-bound runtime dependencies by replacing direct `IServiceProvider` usage in cache, room, chat, and RCON flows with focused resolver/accessor services, leaving dynamic packet-handler activation as the main intentional service-provider boundary.
- Continued the DI/service transition in startup/runtime activation too by moving packet-handler activation behind `IPacketEventActivator` and replacing `Program`'s stored root provider reference with a typed runtime shutdown dependency.
- Closed the remaining `CatalogService` nullability warning by guarding optional permission state during catalog badge-grant checks, restoring a clean `dotnet build` baseline with zero warnings.
- Hardened movement diagnostics and `RoomUser` client resolution so move packets, path recalculation failures, invalid step rejections, and target-square occupancy denials are now logged explicitly while avatar session lookup is retried through the room client manager before movement-state helpers give up.
- Centralized room lifecycle ownership further so `RoomManager` now evaluates unloads after room ticks and on occupancy changes, `Room` no longer unloads itself during idle/crash paths, and room disposal is reduced to resource cleanup instead of also running leave business logic.
- Tightened room exit determinism by routing disconnect and room-entry-finalization failures back through `RoomService`, keeping habbo room-reference cleanup, room-runtime removal, and post-leave unload evaluation on one service-owned boundary.
- Hardened `Habbo.OnDisconnect()` so persistence failures no longer abort the rest of the disconnect lifecycle; runtime cleanup now still runs from `finally`, and disconnect-save logging is explicit.
- Tightened room lifecycle handling so idle unload decisions only reset on real user occupancy, preventing bots and pets from pinning empty rooms in memory.
- Hardened room teardown by clearing attached habbo room references before room-user structures are disposed, reducing stale room-reference crashes during later disconnect cleanup.
- Increased lifecycle observability in room/habbo flow by adding targeted room-prepare, enter, leave, unload, disconnect, and room-user attach/detach logs, and by expanding console exception rendering in `nlog.config`.

### [0.8.2] - 2026-03-28
#### Added
- `WiredExecutionData` to carry queued Wired trigger invocations through the room cycle.
- Logger-backed Wired queue observability so room-local enqueue, batch processing, saturation, and slow-cycle behavior can be traced without attaching a debugger.

#### Changed
- Centralized room idle/promotion/unload lifecycle decisions in `RoomManager` so `Room` now focuses on active-room cycle work instead of duplicating unload logic locally.
- Split the `Room` constructor bootstrap into an explicit room-content initialization step so furniture, map, promotions, rights, filter, bot, and pet loading no longer sit inline with dependency assignment.
- Split `Room` bootstrap again into room-state and room-creature initialization helpers, and extracted bot/pet/rights/filter row-to-model translation so startup loading no longer mixes collection queries with deployment details inline.
- Split `Room.ProcessRoom()` into explicit active-cycle phases so item ticks, user ticks, status serialization, game-item ticks, and Wired ticks no longer sit inside one repeated try/catch ladder.
- Split `RoomManager` cycle/load flow into explicit room-cycle, room-creation, registration, and disposal helpers so lifecycle ownership is no longer buried inside `OnCycle()` and `TryLoadRoom()`.
- Split `Room` crash and dispose flow into explicit user-eviction, process-task, collection-reset, disposable-system, and component-cleanup helpers so room teardown no longer lives in one long shutdown branch.
- Split the room creation path further by making `RoomManager` a clearer cache/register coordinator and moving `RoomFactory` room-row query/mapping resolution behind dedicated helpers instead of repeating projection SQL inline.
- Extracted moved-item persistence out of `RoomItemHandling` into `RoomItemPersistenceService`, so room item tick/state code no longer owns the low-level extra-data, wall-position, and coordinate database writes directly.
- Extracted floor-item placement and `CheckPosItem` validation logic into `RoomItemPlacementValidatorService`, so `RoomItemHandling` now coordinates placement flow instead of also owning the tile, user-blocking, stackability, and height-resolution rule set.
- Extracted floor/wall placement persistence into `RoomItemPlacementPersistenceService`, so `RoomItemHandling` no longer writes room-placement updates directly when applying floor or wall item placement.
- Extracted roller target analysis plus item/user move eligibility checks into `RoomRollerService`, so `RoomItemHandling` keeps packet/state coordination while roller movement rules live behind a dedicated service.
- Extracted owned-item removal ownership checks and inventory re-add behavior into `RoomItemInventoryService`, so `RoomItemHandling` no longer owns those inventory return decisions directly during bulk pickup.
- Extracted queued room-item dequeue/process/requeue flow into `RoomItemUpdateQueueService`, so `RoomItemHandling.OnCycle()` no longer owns the raw update-queue processing loop.
- Extracted furniture load normalization/recovery into `RoomItemLoadService` and room-side removal preparation/broadcast behavior into `RoomItemRemovalService`, so `RoomItemHandling` carries less of the room item load/remove orchestration directly.
- Extracted loaded-item state initialization into `RoomItemStateService`, so roller/hopper flags plus moodlight/toner/wired startup behavior are no longer owned directly by `RoomItemHandling`.
- Extracted floor/wall placement apply orchestration into `RoomItemPlacementApplyService`, so `RoomItemHandling` no longer directly owns placement-side packet, map, tent, user-status, and persistence coordination.
- Extracted loaded-item registration, lookup, remove-from-map, moved/roller tracking, and disposal cleanup into `RoomItemTrackingService`, so `RoomItemHandling` no longer owns most of the raw item-state bookkeeping.
- Extracted roller item/user movement apply plus roller-triggered Wired fanout into `RoomRollerApplyService`, so `RoomItemHandling` no longer directly owns the slide-packet, avatar move, or post-roll Wired trigger application path.
- Unblocked local emulator startup by breaking the remaining eager singleton-resolution traps around room/chat/runtime boot: `RoomFactory`, `RoomManager`, `RconSocket`, `ProcessComponent`, and `ChatManager` now defer their cyclic `RoomService`, `GroupManager`, `CommandManager`, and `CacheManager` dependencies until first use instead of deadlocking during container construction.
- Hardened non-interactive startup failure handling too: `PlusEnvironment` no longer throws a second `Console.ReadKey` exception when database startup fails under redirected input, so local smoke tests now surface the real database-connect error and exit cleanly.
- Fixed `room_models` startup loading against the current schema too: `RoomManager` no longer asks Dapper to materialize `RoomModel` directly from mismatched `public_room`/constructor metadata, and now maps the live `club_only`/`wall_height` row shape into `RoomModel` explicitly.
- Fixed SSO ticket cleanup against the current `users` schema too: login/logout reset paths no longer write `NULL` into the non-null `auth_ticket` column, and now clear tickets with an empty string instead.
- Hardened seasonal calendar loading too: missing `user_xmas15_calendar` tables no longer abort SSO login, and `LoadUserCalendarTask` now logs a warning and falls back to an empty calendar component when that legacy table is absent.
- Split `RoomUserManager.RemoveUserFromRoom()` into explicit notify, habbo reset, mount/team/trade cleanup, persistence, messenger, and disposal phases so room leave/disconnect handling is no longer one long mixed-responsibility branch.
- Split `RoomService` room-transfer and entry authorization flow into explicit leave-current-room, public-room, private-room bypass, doorbell, password, and open-room helper phases so `PrepareRoom()` and `LeaveRoom()` no longer duplicate room-exit behavior or carry the full room-entry decision ladder inline.
- Centralized disconnect ownership back onto the game-server layer by making `GameClient.OnDisconnected()` idempotent, clearing the attached habbo client reference there, and removing the duplicate proxy-level disconnect signal that previously risked double logout handling.
- Split `Authenticator.AuthenticateUsingSSO()` into explicit habbo creation, disconnect-lifecycle hookup, session binding, and login completion phases so the login path no longer mixes session registration, event wiring, and task fanout inline.
- Routed packet-level room exits for hotel-view navigation and username-change room resets through `RoomService.LeaveRoom()` so those flows no longer bypass the shared room-exit coordinator with direct `RoomUserManager.RemoveUserFromRoom(...)` calls.
- Extended `RoomService.LeaveRoom()` with an explicit `notifyUser` option and moved the `GetRoomEntryDataEvent` add-avatar failure path onto that shared coordinator too, so both noisy and silent room-exit packet flows now converge on the same lifecycle boundary.
- Routed moderator kick flows through `RoomService.LeaveRoom()` too, so `ModerationRoomService`, `:kick`, and `:roomkick` no longer bypass the shared room-exit coordinator with direct room-user removal calls.
- Added `RoomService.KickFromRoom()` and moved the remaining room-owner kick packet, room-crash eviction, and cannonball-forced exit paths onto the shared room-exit service boundary so both normal leaves and kick-style removals no longer branch through ad hoc `RemoveUserFromRoom(...)` calls.
- Routed `ModerationActionService.Kick()` through `RoomService.LeaveRoom()` as well, so the remaining moderation kick action no longer bypasses the shared room-exit lifecycle boundary.
- Added packet observability in `GameClient` so unknown incoming packet headers and packet-handler exceptions now log revision, build, session, and user context instead of silently disappearing.
- Named the remaining roller, wired-role, gift, dice, and horse-equipment interaction checks on `ItemDefinition`, so placement, teleporter lookup, gift purchase, horse customization, `MatchPosition`, and Wired dispatch no longer open-code those hot interaction comparisons inline.
- Continued the same item semantics cleanup into item serialization too by naming television and room-decoration extradata roles on `ItemDefinition`, removing another small pocket of raw `InteractionType` switching from `ItemBehaviourUtility`.
- Named room effect-map semantics on `ItemDefinition` as well, so `GameMap` no longer open-codes pool/skates/lowpool/halloweenpool effect IDs inline when rebuilding tile state.
- Named team-gate, arrow, and effect-provider semantics too, so `TeamManager`, `RoomUserManager`, and the remaining `Item` update loops no longer duplicate those interaction-role checks inline.
- Centralized wall-item post-it extradata formatting too, so `ItemBehaviourUtility` and `ItemUpdateComposer` no longer each duplicate the same post-it wall payload trimming rule.
- Started pushing those named item semantics down into `Item` itself too: the interactor picker and a first runtime update slice now use helper predicates for gates, teleporters, rollers, counters, dice, timers, wired reset boxes, and other hot interaction roles instead of repeating raw interaction cases.
- Continued that `Item` runtime cleanup across the teleporter/hopper/gate-vip/banzai update slice too, including a named banzai floor pulse-state helper and a single `IsWired` reset branch instead of three explicit wired role cases.
- Started splitting the `Item.ProcessUpdates()` god-switch itself too by extracting the `Hopper` and `Teleport` runtime branches into dedicated private helpers, reducing one of the densest remaining legacy update blocks without changing behavior.
- Continued breaking down `Item.ProcessUpdates()` by extracting the `OneWayGate`, `GateVip`, `Scoreboard`, `Counter`, and `FreezeTimer` branches plus shared legacy-second parsing, further shrinking the remaining monolithic runtime switch.
- Extracted the cannonball runtime branch too, including its target-square calculation, so the forced-kick path no longer lives inline inside the main `Item.ProcessUpdates()` switch.
- Removed the obsolete `ICatalogManager` service-bag surface by moving `FigureDataManager` to direct `IClothingManager` injection and `UpdateCommand` to direct `IVoucherManager` injection, so catalog runtime consumers no longer reach through deprecated manager properties.
- Removed the now-dead `PlusEnvironment` conversion, timestamp, and username static helpers as well, since those legacy compatibility shims no longer have active callers after the earlier DI and timestamp cleanup.
- Removed another small wave of stale compatibility surface too: `GameClient.PingCount`, `MessengerBuddy.Client`, and `PermissionComponent.Init()` are gone, while the still-active `MachineId` field is no longer incorrectly marked as deprecated.
- Narrowed `PlusEnvironment`'s private static surface too: startup-only services such as settings, figure-data, RCON, and item-data management now live on the instance, leaving only the dependencies still required by the remaining static helpers on the static side.
- Removed the last `PlusEnvironment` encoding/culture passthroughs as well by moving packet decoding to `Encoding.Default` and room-decoration numeric parsing to `CultureInfo.InvariantCulture`, shrinking the remaining global helper surface again.
- Fixed shutdown listener ownership too: `PerformShutDown()` now stops the Nitro websocket server and the RCON listener in addition to the Flash listener, `RconSocket` now exposes an explicit `Stop()` path instead of relying on process exit to drop the socket, and the server-status timer is disposed during shutdown instead of being left running until process exit.
- Tightened `server_status` freshness too: `ServerStatusUpdater` now ticks every second, only writes when connect/disconnect or room load/unload activity marks status dirty, and still performs a slower reconciliation write so the database stays near-real-time without spamming identical updates.
- Removed the last active `PlusEnvironment` runtime-control globals too: shutdown, console alerting, and server-start timestamp now flow through dedicated runtime-control and runtime-state services instead of static environment methods and fields.
- Split more of `Item.ProcessUpdates()` into dedicated runtime helpers so bottle, dice, Habbo wheel, love-shuffler, alert, vending, banzai counter/floor/puck, freeze tile, pressure pad, and wired-reset handling no longer sit inline inside the remaining item update switch.
- Moved the extracted item update handlers out of `Item.cs` and into `HabboHotel/Items/Runtime/Item.RuntimeUpdates.cs`, so the core item model file now carries less runtime behavior noise while the remaining update logic lives in a dedicated partial runtime file.
- Started a broader `Habbo` state refactor by moving client/session attach-detach and room enter/leave ownership onto `Habbo` methods, then shifting core consumers (`GameClient`, `Authenticator`, `RoomUserManager`, `EffectsComponent`, messenger sync, and client whisper helpers) away from raw `Client` / `CurrentRoom` field manipulation.
- Extended that `Habbo` refactor into wired and moderator room flows too by moving summon, wired kick, user-says triggers, and triggerer-on/off-furni checks onto `Habbo` client/room helpers instead of raw field access.
- Continued the same `Habbo` helper migration through messenger follow, chat, and trading service hot paths too, so those flows now resolve the active room through `Habbo.TryGetCurrentRoom()` instead of assuming direct `CurrentRoom` field reads.
- Started a broader incoming packet sweep too: room-bound packet handlers now increasingly resolve the active room through `Habbo.TryGetCurrentRoom()` and the shared `RoomPacketEvent` guard instead of open-coding repeated `CurrentRoom` null checks.
- Extended that packet sweep across additional users/catalog/furni/floorplan handlers too, reducing another batch of direct `CurrentRoom` reads in figure updates, room ads, wired save, gift open, room settings, and floorplan packet flows.
- Continued the packet sweep across furni/action/inventory handlers as well, so one-way gate, dice, guild-furni settings, activated badges, and avatar-effect selection no longer open-code direct `CurrentRoom` reads.
- Continued the same normalization into `RoomAccessService` and the YouTube television packet handlers too, so room-rights/mute/filter actions and TV interaction guards now rely on the shared active-room helper surface.
- Tightened a smaller follow-up packet batch too by moving `KickUserEvent` and `GetRoomEntryDataEvent` onto the same helper path and by removing a few remaining nullable-guard redundancies once the active habbo/room is already established.
- Split `RoomItemHandling` furniture load/remove flow into explicit helper stages so invalid floor recovery, wall-position normalization, registration, and removal broadcast/state cleanup no longer live in one monolithic method.
- Split `RoomItemHandling.SetFloorItem` into explicit placement validation, stack/height resolution, and final apply steps so the room item placement path is easier to reason about without changing behavior.
- Split `RoomItemHandling.SaveFurniture` persistence into dedicated extra-data, wall-position, and coordinate update helpers so moved-item persistence no longer hides three separate write paths in one loop.
- Split `RoomItemHandling.CycleRollers` into roller target analysis plus separate item/user move decisions so the roller cycle no longer buries both movement paths in one nested branch.
- Split `RoomItemHandling.OnCycle` into explicit roller-cycle and queued-item-update helpers so the remaining room item tick flow is easier to follow.
- Split `RoomItemHandling.RemoveItems` into owned-item filtering plus dedicated floor/wall removal helpers so bulk inventory return no longer mixes ownership, collection mutation, inventory updates, and packet broadcast inline.
- Split `RoomItemHandling.CheckPosItem` into explicit tile, door, height, user, and stackability checks so game-item movement validation no longer lives in one long try block.
- Split `RoomItemHandling.SetWallItem`, small item-tracking helpers, and `Dispose` cleanup into explicit phases so the remaining wall placement and teardown code is easier to follow.
- Split the remaining `RoomItemHandling` utility branches as well: wall-position pair parsing, single-item removal preparation, roller user Wired trigger fanout, simple toner initialization, and loaded item lookup are now all helper-based.
- Polished `RoomItemHandling` naming/grouping consistency too by introducing a shared default wall-position constant, a loaded-item lookup helper, and a per-item floor initialization helper instead of repeating tiny inline branches.
- Moved non-chat `WiredComponent.TriggerEvent` execution onto a bounded per-room queue processed during `WiredComponent.OnCycle()`.
- Extended the queue-based Wired execution slice to cover matched `TriggerUserSays` and `TriggerUserSaysCommand` boxes without breaking the existing synchronous suppression semantics.
- Moved `TriggerUserSays` and `TriggerUserSaysCommand` match resolution fully into `WiredComponent`, leaving the queued trigger boxes responsible only for owner checks, feedback whispers, and stack execution.
- Added `WiredChatTriggerContext` so queued chat and command triggers no longer depend on raw positional `object[]` payloads for their actor/message handoff.
- Added `WiredActorItemTriggerContext` for walk-on, walk-off, furni-collision, and state-change triggers so the next queued trigger slice also moves off raw positional payload unpacking.
- Added `WiredActorTriggerContext` for `TriggerRoomEnter` and explicit parameterless queue handling for game start/end triggers so the remaining low-parameter trigger paths also stop relying on ad hoc payload packing.
- Added `WiredContextResolver` and moved repeated actor / actor-item / chat payload unpacking in several trigger, condition, and effect boxes onto the shared helper surface.
- Extended `WiredContextResolver` adoption across the remaining actor-driven triggerer/team/hand-item boxes, further shrinking direct `Habbo` casts spread through Wired conditions and effects.
- Moved the remaining actor-only tail set (`TeleportUser`, nested stacks, badge rewards, and bot follow/hand-item effects) onto the shared context resolver so the queue-backed trigger path now lands on a much smaller raw-cast surface.
- Added `WiredSetItemSelector` so bot move/teleport and user teleport effects share the same random in-room furni selection and stale-item pruning logic instead of duplicating it per box.
- Removed remaining no-op `@params` guards from parameterless bot/user-count/state-match boxes so they no longer pretend to depend on trigger payloads they never read.
- Added `WiredConditionDataParser` so user-count and state/position condition boxes no longer each re-split and re-parse the same `StringData` payload inline.
- Added `WiredTeamParser` so actor/team condition and effect boxes no longer repeat manual team-id decoding or effect-id math inline.
- Added `WiredFurniSnapshotParser` so `MatchPosition` and the state/position condition boxes share the same saved furni snapshot decode path instead of each re-splitting `ItemsData` entries.
- Added `WiredExecutionAdapter` so `WiredComponent` no longer calls legacy `Execute(...)` entry points directly and instead goes through a shared bridge while the old interface remains in place.
- Added `IWiredExecutable` and `WiredExecutionContext`, then migrated the first typed slice (`UserSays`, `UserWalksOn`, `ShowMessage`) onto that bridge without removing the legacy `Execute(params object[])` contract yet.
- Expanded the typed execution slice to additional actor-driven trigger/effect boxes (`UserSaysCommand`, `UserWalksOff`, `RoomEnter`, `MuteTriggerer`, `KickUser`) so more of the common Wired path now runs through `WiredExecutionContext`.
- Extended the typed execution slice again across actor-item triggers and representative condition/effect boxes (`StateChanges`, `UserFurniCollision`, `IsGroupMember`, `IsWearingBadge`, `GiveUserBadge`).
- Expanded typed execution coverage across the mirrored actor-condition set as well (`IsNotGroupMember`, `IsNotWearingBadge`, `IsWearingFx`, `IsNotWearingFx`), further reducing raw `params object[]` usage in the condition layer.
- Moved the remaining triggerer/team/hand-item actor boxes (`TriggererOnFurni`, `TriggererNotOnFurni`, `ActorHasHandItem`, `ActorIsInTeam`, `AddActorToTeam`, `RemoveActorFromTeam`) onto `IWiredExecutable`, extending the typed execution path across another cohesive actor-driven slice.
- Extended the typed execution bridge across the remaining actor-targeted effect slice too (`TeleportUser`, `ExecuteWiredStacks`, `BotFollowsUser`, `BotGivesHandItem`), so more queued and nested effect flow now bypasses raw payload unpacking.
- Moved the simple parameterless/data-only slice (`GameStarts`, `GameEnds`, `AddonRandomEffect`, `RegenerateMaps`, `SetRollerSpeed`, `BotCommunicatesToAll`) onto the same typed bridge so the new execution path is no longer limited to actor-carrying boxes.
- Moved the furni occupancy condition slice (`FurniHasUsers`, `FurniHasNoUsers`, `FurniHasFurni`, `FurniHasNoFurni`) onto `IWiredExecutable` too, broadening typed execution coverage across non-actor conditions.
- Moved the remaining user-count and state/position condition boxes (`UserCountInRoom`, `UserCountDoesntInRoom`, `FurniMatchStateAndPosition`, `FurniDoesntMatchStateAndPosition`) onto the typed execution bridge as well.
- Moved the remaining bot-targeted/data-only effect boxes (`BotMovesToFurni`, `TeleportBotToFurni`, `BotChangesClothes`, `GiveReward`) onto the typed execution bridge too.
- Moved the remaining cycle-backed execution boxes (`Repeater`, `ToggleFurni`, `MoveFurniToUser`, `MoveAndRotate`, `MatchPosition`) onto `IWiredExecutable`, so the typed bridge now spans queued, delayed, and cycle-driven paths too.
- Promoted `IWiredExecutable` and `WiredExecutionContext` into the main public Wired contract, made `IWiredItem` extend that typed surface, and marked the old `Execute(params object[])` signature as a legacy bridge entry point.
- Simplified `WiredExecutionAdapter` so internal Wired dispatch now goes straight through `WiredExecutionContext` without runtime fallback branching to the legacy variadic path.
- Added a default legacy bridge implementation on `IWiredItem` itself and removed the redundant `Execute(params object[])` forwarding boilerplate from the first low-risk parameterless/data-only box slice.
- Removed the same legacy forwarding boilerplate from a much larger typed execution slice across user/chat triggers plus actor-driven effects, cutting the remaining per-box wrappers down substantially.
- Removed the last remaining per-box legacy forwarding wrappers as well, so Wired boxes now rely on the shared interface bridge instead of carrying duplicate `Execute(params object[])` boilerplate individually.
- Removed the legacy variadic `IWiredItem.Execute(params object[])` contract entirely, leaving `IWiredExecutable.Execute(WiredExecutionContext)` as the sole Wired execution entry point.
- Started splitting the broad typed execution context too: queued `UserSays` and `UserSaysCommand` execution now travels through a dedicated `WiredChatExecutionContext` instead of the generic parameter-based context path.
- Continued that context split for actor+item triggers as well: queued walk/collision/state-change execution now uses a dedicated `WiredActorItemExecutionContext`.
- Extended the split to actor-only paths too: queued actor triggers plus the shared trigger-stack / nested-stack actor execution flow now run through a dedicated `WiredActorExecutionContext`.
- Added a dedicated empty execution context for parameterless Wired paths and removed the now-unused `Parameters` / `CommandManager` baggage from `WiredExecutionContext`.
- Carried that context split up to the executable interface layer too by adding specialized chat / actor-item / empty executable contracts and routing the first box families through them.
- Continued the interface specialization for actor-only condition boxes as well, so the common badge/fx/group/team/triggerer checks now dispatch through a dedicated actor executable contract.
- Finished the same actor-only specialization across the remaining triggerer and actor-driven effect boxes, so the broad base executable interface is now mostly a compatibility shell rather than the hot path.
- Moved the specialized `IWiredExecutable` bridge implementations onto the specialized executable interfaces themselves (`IWiredChatExecutable`, `IWiredActorItemExecutable`, `IWiredActorExecutable`, `IWiredEmptyExecutable`), which removes another large block of per-box explicit bridge boilerplate without changing dispatch behavior.
- Removed the remaining explicit broad-interface bridge shims from the actor-specialized trigger/effect tail too (`TriggererOnFurni`, `TriggererNotOnFurni`, `ShowMessage`, `KickUser`, `GiveUserBadge`, `ExecuteWiredStacks`, `TeleportUser`, `BotGivesHandItem`, `BotFollowsUser`, `AddActorToTeam`, `RemoveActorFromTeam`, `MuteTriggerer`), leaving only the still-generic boxes on the unspecialized bridge path.
- Moved the last still-generic room-enter, user-count, furni-state, furni-occupancy, and cycle-heavy boxes onto specialized actor/empty execution contracts as well, so per-box explicit `IWiredExecutable.Execute(WiredExecutionContext)` implementations are now gone entirely from the Wired box layer.
- Removed the now-redundant `IWiredExecutable` interface declarations from Wired box class signatures too, since `IWiredItem` already inherits that base contract and the boxes now advertise only the narrower execution interfaces they actually implement.
- Removed the remaining raw `object[]` queue payload handling from the main Wired dispatch path too: queued trigger execution now carries either a typed trigger context or no context at all, and the old generic parameter-based adapter helpers are gone.
- Marked `WiredExecutionContext` itself as an abstract base type as well, reflecting the fact that execution now always flows through one of the narrower specialized context shapes rather than directly instantiating the shared shell.
- Added a shared `WiredEffectDataParser` for the remaining bot/mute/move-rotate string payloads too, which removes another pocket of repeated `Split(';')` / integer parsing logic from the effect boxes.
- Added shared state/position snapshot enumeration for the `MatchPosition` and furni state/position condition family as well, so those boxes now reuse the same decoded snapshot stream and mode parser instead of each re-splitting the saved payloads on their own.
- Added a `WiredBotDataParser` for the remaining bot-targeted effect boxes too, so simple bot-name and bot-clothing payload decoding no longer lives inline across `BotChangesClothes`, `BotMovesToFurni`, `TeleportBotToFurni`, and `BotCommunicatesToAll`.
- Simplified `MatchPosition` execution flow as well by collapsing its per-mode apply/logging branches into one shared snapshot-application path, which trims another chunk of duplicated control flow from the cycle effect tail.
- Added a shared `WiredFloorMoveHelper` for the repeated roll/place/slide logic in `MoveAndRotate` and `MoveFurniToUser`, so that floor-item movement validation and placement now lives in one helper instead of two near-identical cycle bodies.
- Simplified queued trigger dispatch in `WiredComponent` as well by centralizing the common enqueue guard and context-based execution routing, which trims another block of repeated branching from the main Wired runtime path.
- Completed the previously stubbed `BotCommunicatesToAll` effect too: it now persists bot/message/mode data, publishes either chat or shout packets with the bot bubble, and notifies bot AI listeners through the matching speech path.
- Tightened `WiredComponent` lookup helpers as well by centralizing typed trigger-box and same-tile box enumeration, which removes another small block of repeated `_wiredItems` filtering from the runtime path.
- Moved queued context dispatch into `WiredExecutionAdapter` too, so `WiredComponent` no longer carries its own chat/actor/actor-item switch and the execution adapter owns the full queued dispatch handoff.
- Finished another small condition-tail cleanup by centralizing single-value numeric parsing in `WiredConditionDataParser`, removing the last direct `int.Parse(StringData)` checks from the actor-hand-item and FX condition boxes.
- Finished the last broad packet-save cleanup pass as well by replacing the remaining unused `var unknown = packet.ReadInt();` placeholders across the Wired boxes with explicit discard reads, which removes another wave of no-op local variables from the save handlers.
- Finished a final readability pass over the new Wired helper layer too by simplifying the parser helpers and queued-dispatch adapter flow into more direct, less assignment-heavy code without changing behavior.
- Consolidated repeated trigger condition/effect execution flow in `WiredComponent` so room-enter, walk, collision, state-change, and game-start/end triggers share the same stack runner helpers.
- Moved repeater and nested wired-stack execution loops onto the same centralized `WiredComponent` helper surface to reduce duplicate trigger/effect traversal logic.
- Fixed `MatchPositionBox` guard logic so removed items are skipped correctly and saved state payloads no longer read past the parsed coordinate data.
- Reworked `MatchPositionBox` saved-state parsing to decode mode flags and saved target payloads once with `TryParse` guards instead of repeatedly splitting and exception-driving the control flow.
- Added `WiredCycleScheduler` and moved common delayed-cycle scheduling logic in `ToggleFurniBox`, `MoveFurniToUserBox`, `MoveAndRotateBox`, and `MatchPositionBox` onto the shared helper surface.
- Normalized `TeleportUserBox` and `KickUserBox` around typed per-box queues and shared delay semantics so the remaining queued Wired effect boxes no longer depend on legacy non-generic queue handling.
- Added `MarkRequested` / `Schedule` helpers to `WiredCycleScheduler` so delayed effect boxes no longer repeat the same next-tick scheduling boilerplate inline.
- Updated `WIRED-ASYNC-PLAN.md` to reflect the first implemented queue-based Wired execution slice.

### [0.8.1] - 2026-03-28
#### Added
- `WIRED-ASYNC-PLAN.md` to document the queued single-threaded execution plan for future Wired async work.

#### Changed
- Routed disconnect handling through session proxies and instance-backed habbo persistence methods instead of direct server-side static persistence calls.
- Continued the in-progress room and habbo lifecycle refactor by reshaping `RoomFactory`, `RoomManager`, `Room`, and `Habbo` around injected database and manager access.
- Updated username, chat-style, avatar-effect, moodlight, toner, and furniture-interaction packet paths to pass `IDatabase` explicitly through the new persistence flow.

### [0.8.0] - 2026-03-28
#### Added
- `IItemTeleporterFinder` and `IItemHopperFinder` interfaces for modernize item discovery.
- Injected `IBadgeManager` and `IUserDataFactory` into `Room` and `RoomManager` for improved service access.
- `LoadUserCalendarTask` to separate advent calendar loading from the legacy user bootstrap path.

#### Changed
- Modernized `ItemTeleporterFinder` and `ItemHopperFinder` to use DI and Dapper.
- Refactored `GiveUserBadgeBox` and `BotChangesClothesBox` to remove static `PlusEnvironment` dependencies.
- Moved avatar effect creation logic from static factory to `EffectsComponent` instance method.
- Decoupled `AmbassadorAlertEvent` and `PetLocale` from static global state.
- Modernized `ReloadServerSettingsCommand`, `MoodlightData`, `CalendarComponent`, and talent track loading to use injected services and Dapper-backed persistence paths.

### [0.7.0] - 2026-03-27
#### Added
- `IBotUtility` and `IPetUtility` services to replace static `BotUtility` and `PetUtility` helpers.
- `UpdateUses` to `IVoucherManager` to centralize voucher usage persistence.

#### Changed
- Refactored `CatalogService` to use injected utilities instead of static classes.
- Modernized `CheckGnomeNameEvent`, `CheckPetNameEvent`, and `PurchaseFromCatalogAsGiftEvent` to support DI.
- Moved inventory-related database logic out of the `Voucher` data model into the service layer.
- Cleaned up remaining `PlusEnvironment.DatabaseManager` calls in the Catalog domain.

### [0.6.0] - 2026-03-27

### [0.6.0] - 2026-03-27
#### Added
- `IItemService` and `ItemService` for centralized furniture interaction logic (Place, Move, Pickup).
- `IItemLoader` and `ItemLoader` service-based implementation (non-static) with Dapper.

#### Changed
- Refactored `PlaceObjectEvent`, `MoveObjectEvent`, `PickupObjectEvent`, and `MoveWallItemEvent` to utilize `IItemService`.
- Modernized `ItemFactory` to use injected `IDatabase` instead of static `PlusEnvironment`.
- Updated `Room`, `RoomManager`, and `RoomItemHandling` to receive `IItemLoader` via dependency injection.
- Refactored `EmptyItems` chat command to use `IItemLoader` and support DI.

### [0.5.0] - 2026-03-26

#### Batch 2 — Loader / Finder Helpers
- `HabboHotel/Users/Inventory/Bots/BotLoader.cs` — Dapper `Query`, parameterized.
- `HabboHotel/Users/Inventory/Pets/PetLoader.cs` — N+1 query optimized to single JOIN.
- `HabboHotel/Items/ItemTeleporterFinder.cs` — `QueryFirstOrDefault`, `System.Data` removed.
- `HabboHotel/Items/ItemHopperFinder.cs` — `QueryFirstOrDefault`.
- `HabboHotel/Items/Interactor/InteractorHopper.cs` — `Execute`, parameterized DELETE.
- `HabboHotel/Items/Interactor/InteractorMannequin.cs` — `Execute`, parameterized UPDATE.

#### Batch 3 — Talent / Quest / Reward / Process
- `HabboHotel/Talents/TalentTrackManager.cs` — `Query`, `System.Data` removed.
- `HabboHotel/Talents/TalentTrackLevel.cs` — `Query` with parameters.
- `HabboHotel/Quests/QuestManager.cs` — `Query`/`Execute`, all string-interpolated SQL injection risks fixed.
- `HabboHotel/Rewards/RewardManager.cs` — split single `dbClient` into multiple `Connection()` scopes.
- `HabboHotel/Users/Process/ProcessComponent.cs` — `Execute`, respect-points calculation optimized.

#### Batch 4 — Catalog / Permissions
- `HabboHotel/Permissions/PermissionManager.cs` — 5 separate connections consolidated to 1.
- `HabboHotel/Catalog/Pets/PetRaceManager.cs` — `SELECT *` → explicit columns.
- `HabboHotel/Catalog/Utilities/BotUtility.cs` — `InsertQuery()` → `ExecuteScalar<long>` + `LAST_INSERT_ID()`.
- `HabboHotel/Catalog/Utilities/PetUtility.cs` — same pattern; parameter names cleaned.
- `HabboHotel/Catalog/Vouchers/Voucher.cs` — `Execute`, parameterized.
- `HabboHotel/Catalog/Vouchers/VoucherManager.cs` — `Query`.

#### Batch 5 — Items
- `HabboHotel/Items/ItemFactory.cs` — 7 `GetQueryReactor` blocks, all `InsertQuery()` → `ExecuteScalar<long>` + `LAST_INSERT_ID()`.
- `HabboHotel/Items/ItemLoader.cs` — `Query`; `out var` → `out ItemDefinition?` to fix CS8197; `data!` null-forgiving.
- `HabboHotel/Items/Data/Toner/TonerData.cs` — single `Connection()` with `QueryFirstOrDefault` + `Execute`; ordinal indexing → named columns.
- `HabboHotel/Items/Wired/Boxes/Effects/BotChangesClothesBox.cs` — `Execute`, parameterized UPDATE.

#### Batch 6 — Marketplace / Wired / Server Status
- `HabboHotel/Catalog/Marketplace/MarketplaceManager.cs` — `AvgPriceForSprite` 2×`QueryFirstOrDefault<int>`, `@spriteId`.
- `Communication/Packets/Outgoing/Marketplace/MarketPlaceOwnOffersComposer.cs` — `Query` + `QueryFirstOrDefault`; `DataTable` → `List<dynamic>`; `System.Data` removed.
- `HabboHotel/Rooms/Instance/WiredComponent.cs` — `LoadWiredBox`: `QueryFirstOrDefault<dynamic?>`; `SaveBox`: `Execute` + anonymous object; `SELECT *` → explicit columns; `System.Data` removed.
- `Core/ServerStatusUpdater.cs` — both `Dispose` and `UpdateOnlineUsers` → `Execute` + anonymous object.

#### Batch 7 — RCON Commands + Packet Handlers
- RCON: `GiveUserCurrencyCommand`, `TakeUserCurrencyCommand`, `ReloadUserCurrencyCommand`, `SyncUserCurrencyCommand` — 4 separate connections each → single `Connection()` per command with switch-case `Execute`/`QueryFirstOrDefault<int>`.
- RCON: `ReloadUserMottoCommand`, `ReloadUserRankCommand`, `ReloadUserVIPRankCommand` — `GetString`/`GetInteger` → `QueryFirstOrDefault<string/int>`.
- Packets: `OpenGiftEvent` — 5 reactors → `QueryFirstOrDefault<dynamic>` + `Execute`; `DataRow` ordinal → named columns; string-interpolated DELETEs → parameterized.
- Packets: `DeleteRoomEvent` — 6 string-interpolated `RunQuery` → `Execute` + `@params`.
- Packets: `RedeemVoucherEvent` — `DataRow` null-check → `QueryFirstOrDefault`; `System.Data` removed.
- Packets: `FriendFurniConfirmLockEvent`, `GetMarketplaceItemStatsEvent` — `Execute`/`QueryFirstOrDefault<int?>`.
- Packets: `CancelQuestEvent`, `StartQuestEvent`, `GetCurrentQuestEvent` — compound interpolated queries → separate `Execute` + `@params`.
- Packets: `GiveRoomScoreEvent`, `ModifyWhoCanRideHorseEvent`, `ChangeMottoEvent` — interpolation → `Execute` + `@params`.
- Packets: `ApplyDecorationEvent`, `UseFurnitureEvent` — `Execute`; column-name interpolation retained with enum safety comment.
- Packets: `SaveFloorPlanModelEvent` — `GetRow`+`AddParameter` chain → `QueryFirstOrDefault` + 2×`Execute`.
- Packets: `CreditFurniRedeemEvent`, `SetTonerEvent`, `DeleteStickyNoteEvent` — `Execute` + `@id`.
- Packets: `SaveRoomSettingsEvent` — 22-parameter `AddParameter` chain → single anonymous object.
- Packets: `SetUserFocusPreferenceEvent`, `UpdateFigureDataEvent` — `Execute` + anonymous object; string interpolation removed.
#### Phase 1 — Catalog Service
- Defined `ICatalogService` and implemented `CatalogService` to encapsulate complex business logic.
- `PurchaseFromCatalogEvent.cs` (374 lines) -> Moved all logic to `CatalogService.PurchaseItem`, reducing packet handler to 29 lines. Logic split into maintainable methods.
- `RedeemVoucherEvent.cs` -> Moved voucher redemption logic to `CatalogService.RedeemVoucher`.
- Added strict null-safety checks to internal service methods.

#### Phase 2 — Quest Service
- Defined `IQuestService` and implemented `QuestService` to centralize quest business logic.
- `QuestManager.cs` simplified to be a data-only manager (Repository pattern).
- Refactored 15+ Packet Handlers to use `IQuestService` via Dependency Injection.
- Migrated all quest progress triggers (Furni interaction, Room entry, Respect, etc.) to the new service layer.
- Added support for async/await in quest progress tracking.
- Maintained legacy compatibility via `IGame.QuestService` for non-DI classes.
- Fixed `CS8602` and `CS4032` build errors during migration.

#### Phase 3 — Achievement Service & Deep Integration
- Completed the migration of `AchievementManager` to `IAchievementService`.
- Refactored `CatalogService.cs` and `RoomAccessService.cs` to use `IAchievementService` via Dependency Injection.
- Modernized legacy code in `Habbo.cs`, `Room.cs`, `BattleBanzai.cs`, and `ProcessComponent.cs` to use `IAchievementService` (fire-and-forget for synchronous paths).
- Updated 10+ Packet Handlers (Ban, Kick, Mute, Ignore, SaveRoomSettings, PlaceObject, etc.) to use `IAchievementService` and `async/await`.
- Converted synchronous methods like `RoomAccessService.GetRoomFilterList` to asynchronous `Task` returning methods.
- Cleared remaining static dependencies on `PlusEnvironment.Game.AchievementManager` in favor of Service-based access.

#### Phase 4 — Chat Service & Moderation Cleanup
- Created `IChatService` and `ChatService` to unify `Chat`, `Shout`, `Whisper`, and Typing status logic.
- Centralized flood control, word filtering, and auto-ban logic into `ChatService.cs`.
- Refactored `ChatEvent.cs`, `ShoutEvent.cs`, `WhisperEvent.cs`, `StartTypingEvent.cs`, and `CancelTypingEvent.cs` to thin packet handlers using DI.
- Refactored `IModerationManager` and `ModerationManager` into pure caching repositories.
- Moved ban-writing implementation into `IModerationActionService` to decouple data and business logic.
- Modernized `BanCommand`, `IpBanCommand`, and `MipCommand` to use `IModerationActionService` and `async/await`.
- Added support for offline user banning via database lookups.

#### Phase 5 — Room Service & Entity Management
- Created `IRoomService` and `RoomService` to centralize room lifecycle, entry, and creation logic.
- Migrated legacy `Habbo.PrepareRoom` and `Habbo.EnterRoom` methods to the new service layer.
- Refactored `OpenFlatConnectionEvent.cs` and `GoToFlatEvent.cs` to use the modernized service via Dependency Injection.
- Centralized room access checks, doorbell handling, and occupancy verification.
- Integrated `IRoomService` into `Item.cs` (Teleport/Hopper) and `RoomUserManager.cs` (Wired Teleport) for consistent room switching.
- Modernized all `IChatCommand` implementations to return `Task` and support `async/await` execution.
- Updated `GOTOCommand`, `SummonCommand`, and `FollowCommand` to use the new service katmanı.
- Reduced the burden on the `Habbo` entity by removing 200+ lines of business logic.

## 2026-03-26

### Legacy Database Wrapper Migration

- Refactored `PlusEnvironment.cs` static DB access methods to use `DatabaseManager.Connection()` with Dapper.
- Moved `HabboHotel/Groups/Group.cs` group initialization and member queries off the legacy wrapper.
- Moved `HabboHotel/Rooms/RoomUserManager.cs` user count and pet/bot updates off the legacy wrapper.
- Moved room model loading and room creation persistence in `HabboHotel/Rooms/RoomManager.cs` to `Connection()`/Dapper.
- Refactored `HabboHotel/GameClients/GameClientManager.cs` chatlog reporting and inventory disconnect-saves to Dapper.
- Converted `HabboHotel/Users/Messenger/SearchResultFactory.cs` user search to `Connection().Query()` using Dapper.
- Replaced `GetQueryReactor()` in `HabboHotel/Subscriptions/SubscriptionManager.cs` with Dapper-powered initialization.
- Completely migrated all Chat Commands (`HabboHotel/Rooms/Chat/Commands/*`) for standard Users, Fun, Moderators, and Administrators to `DatabaseManager.Connection()` with Dapper, resolving over 20 files.


## 2026-03-25

### Nitro Handshake Diagnostics

- Moved runtime revision loading from `revisions/` and `Resources/Revisions` conventions to `Config/Revisions`, and updated the project to copy revision JSON files into the build output `Config` tree.
- Added richer Nitro handshake diagnostics covering client-hello acceptance, unknown revision reporting, Diffie/secret-key/unique-id/SSO handshake stages, disconnect reasons, and unhandled packet logging.
- Hardened websocket/TCP session logging against disposed-socket crashes and added shutdown reason / unhandled exception logging so handshake investigation no longer terminates the emulator.

## 2026-03-24

### Build Cleanup

- Moved `AvatarEffectFactory` and `PetLocale` off the legacy query wrapper by inserting avatar effects and loading pet locale responses through `DatabaseManager.Connection()`/Dapper.
- Moved `BansComponent` and `FilterComponent` off the legacy query wrapper by persisting room bans and room filter updates through `DatabaseManager.Connection()`/Dapper.
- Moved `ClothingComponent` and `CalendarComponent` off the legacy query wrapper by loading user clothing parts and advent calendar state through `DatabaseManager.Connection()`/Dapper.
- Moved avatar effect loading and persistence off the legacy query wrapper by switching `EffectsComponent` and `AvatarEffect` to `DatabaseManager.Connection()`/Dapper for load, activate, expire, and quantity update flows.
- Moved `RoomItemHandling` load/save/place persistence off the legacy query wrapper by updating item owner, room, wall-position, and extra-data writes through `DatabaseManager.Connection()`/Dapper.
- Moved `RoomFactory` room-data bootstrap loading off the legacy query wrapper by resolving room rows through `DatabaseManager.Connection()`/Dapper while keeping the existing room-manager/model lookups intact.
- Moved `Room` bot/pet bootstrap plus rights/filter loading off the legacy query wrapper by resolving room bootstrap data through `DatabaseManager.Connection()`/Dapper.
- Replaced the remaining `Habbo` save/disconnect query-wrapper usage with `DatabaseManager.Connection()`/Dapper, keeping the existing global manager orchestration intact while removing legacy `GetQueryReactor()` persistence calls.
- Moved `MoodlightData` persistence off the legacy query wrapper and `PlusEnvironment` bool helpers, loading and updating moodlight presets through `DatabaseManager.Connection()` plus `ConvertExtensions`.
- Moved `GameDataManager` game bootstrap loading off the legacy DB wrapper and away from `PlusEnvironment.EnumToBool`, mapping game configuration through `Connection()`/Dapper plus `ConvertExtensions`.
- Moved `ItemDataManager` furniture bootstrap loading off the legacy DB wrapper by mapping item definitions directly through `Connection()`/Dapper.
- Moved `NavigatorManager` category and featured-room bootstrapping off the legacy DB wrapper by loading navigator metadata through `Connection()`/Dapper.
- Replaced the legacy static `NavigatorHandler` with an injected `NavigatorQueryService`, moving navigator search result resolution off `PlusEnvironment.Game` / `DatabaseManager` and into a dedicated query layer used by `NavigatorSearchResultSetComposer`.
- Moved `ModerationManager` off the legacy DB wrapper and global timestamp helper by switching preset loading, ban cache rebuilds, and ban persistence/checks onto `Connection()`/Dapper plus `UnixTimestamp.GetNow()`.
- Moved `GroupManager` off the legacy DB wrapper by switching group item loading, group lookup/creation, and user-group listing onto `Connection()`/Dapper.
- Removed the last legacy DB-wrapper usage from the extracted moderation service layer by moving `ModerationRoomService` updates and `ModerationQueryService` user-info reads onto `Connection()`/Dapper, and by decoupling `ModeratorUserInfoComposer` from `DataRow` inputs.
- Removed the remaining legacy `GetQueryReactor()` calls from `RoomCreatureService`, moving pet and bot persistence/speech queries onto `Connection()`/Dapper, and verified that `NavigatorService` no longer depends on legacy global or DB wrapper lookups.
- Replaced the remaining `GetQueryReactor()` usage in `AvatarClothingService` and most read-model query paths in `ModerationQueryService` with `Connection()`/Dapper, keeping the legacy moderator user-info `DataRow` path intact for composer compatibility.
- Replaced `GetQueryReactor()` usage with `Connection()`/Dapper in the extracted `MarketplaceService` and `RoomAccessService` DB paths, reducing legacy database access in the new service layer.
- Reduced legacy global lookups in the extracted group and moderation services, replacing `PlusEnvironment.GetHabboById` and direct global timestamp access with injected managers/utilities in the new service layer.
- Extracted the room pet and bot packet flow into a dedicated `RoomCreatureService`, moving placement, pickup, horse effects, info/training, and bot action orchestration out of room AI packet handlers.
- Extracted the wardrobe and clothing packet flow into a dedicated `AvatarClothingService`, moving wardrobe load/save, sellable clothing redemption, and mannequin state orchestration out of avatar and furni packet handlers.
- Extracted the room rights and access packet flow into a dedicated `RoomAccessService`, moving rights, doorbell, bans, mute-tool, room-filter, and enforced-category orchestration out of room packet handlers.
- Extracted the marketplace packet flow into a dedicated `MarketplaceService`, moving make-offer, buy, browse, own-offers, can-make-offer, redeem, and cancel orchestration out of marketplace packet handlers.
- Extracted the moderation room batch into a dedicated `ModerationRoomService`, moving room lock/name cleanup, tag/promotion reset, and kick-all orchestration out of the moderation packet handler.
- Extracted the moderation query batch into a dedicated `ModerationQueryService`, moving moderator user info, room info, room visits, and chatlog read-model orchestration out of moderation packet handlers.
- Extracted the moderation ticket batch into a dedicated `ModerationTicketService`, moving ticket submission, pick/release, close, and pending-call orchestration out of moderation packet handlers.
- Extracted the first moderation action batch into a dedicated `ModerationActionService`, moving caution, alert, mute, kick, ban, trade-lock, and room-wide moderator action orchestration out of moderation packet handlers.
- Extracted the group packet flow into a dedicated `GroupService`, moving membership, favourite group, admin rights, identity/settings, badge/colour, purchase, and deletion orchestration out of packet handlers.
- Extracted the navigator packet flow into a dedicated `NavigatorService`, moving room creation, favourites, promotion editing, search setup, guest-room loading, and navigator preference orchestration out of packet handlers.
- Extracted the messenger and friend-list packet flow into a dedicated `MessengerService`, moving request, invite, message, search, follow, and relationship orchestration out of packet handlers.
- Extracted the trading packet flow into a dedicated `TradingService`, keeping trading packet handlers thin and moving trading state, validation, and persistence orchestration out of the packet layer.
- Finished the remaining nullable warning sweep across catalog, trading, room entry, voucher, clothing, permission, moderation, and user component flows, bringing the project build to `0 Warning(s), 0 Error(s)`.
- Eliminated all remaining `CS8602` nullable dereference warnings across packet flows, marketplace, wired boxes, quest flow, and related room helpers, reducing the project warning count to `26` with `0 Error(s)`.
- Cleaned the solution build output to `0 Warning(s), 0 Error(s)`.
- Switched `PluginExample` to a project reference so the solution builds without `PLUS_EMULATOR_HOME`.
- Removed the Linux-hostile pre-build echo target and suppressed legacy warning categories at the project level.
- Removed two unused exception variables in the game client layer.
- Continued the warning cleanup with broad null-safety and repeated access refactors across incoming packets, room logic, AI, and command handlers.
- Kept the solution compiling cleanly after each cleanup batch, ending with `0 Warning(s), 0 Error(s)` on the full Release build.

### Runtime And Framework

- Continued the post-packet `Habbo` cleanup in the chat command layer by routing `CommandManager` and a first batch of moderator/user commands away from direct `CurrentRoom` / `Client` reads and onto the newer helper methods.
- Extended that chat-command cleanup across moderator notification utilities, same-room fun commands, summon/user-info flows, and currency/trade-ban actions so a larger slice of the command layer now prefers `Habbo` room/client helpers over raw property reads.
- Finished the command-folder `Habbo` helper sweep by moving the last admin group-deletion room lookup onto `Habbo.TryGetCurrentRoom(...)`, leaving the command layer off direct `CurrentRoom` / `Client` reads.
- Continued the post-packet/post-command `Habbo` cleanup into supporting services by moving ignores sync, ambassador warnings, badge/effect notifications, marketplace offer cancellation, and clothing room lookups onto `Habbo.TryGetClient(...)` / `Habbo.TryGetCurrentRoom(...)`.
- Pushed the same `Habbo` helper cleanup into moderation, trading, room entry/exit, support-ticket, and process-tick services so active-room and attached-client resolution in those runtime paths now leans on `TryGetCurrentRoom(...)` / `TryGetClient(...)` instead of raw property reads.
- Continued the same helper migration into navigator friend-room lookup plus a small interactor/Wired effect tail, trimming another batch of direct `CurrentRoom` / `Client` reads from runtime code outside packets and commands.
- Moved the remaining `RoomUserManager` hot-path room ownership checks and team-gate lookups onto `_room` / `Habbo` helper boundaries, trimming another slice of direct `CurrentRoom` coupling out of user-cycle logic.
- Reduced `TeamManager` gate-update coupling as well by moving its room resolution behind a shared `Habbo.TryGetCurrentRoom(...)` helper instead of re-reading raw `CurrentRoom` in each game branch.
- Started the heavier `RoomCreatureService` helper migration too by moving the first pet/bot entry points off raw `CurrentRoom` reads and onto `TryGetCurrentRoom(...)` or the already-owned room instance.
- Trimmed the broader Habbo-helper tail as well by dropping redundant `InRoom`/`CurrentRoom` checks in messenger follow, quests, chat, freeze-tile interaction, chat-trigger Wired boxes, and Banzai movement handling in favor of direct `TryGetCurrentRoom(...)` resolution.
- Finished the navigator-side friend-room cleanup by adding a small `MessengerBuddy.TryGetCurrentRoom(...)` helper and routing the last remaining raw buddy-room lookup through it.
- Removed the old public room-state compatibility surface from `Habbo` and `MessengerBuddy`, leaving room resolution on explicit helper methods instead of exposed `CurrentRoom` / `InRoom` properties.
- Started moving the post-packet Habbo cleanup into services by trimming `GroupService` room/client access repetition, routing favourite-group refresh through helpers, and swapping controller notifications onto `Habbo.TryGetClient(...)`.
- Continued trimming the packet tail by tightening handshake/profile/gift guards, extracting the repeated room-settings broadcast path, and splitting the item-collection phase out of room deletion before deeper service cleanup.
- Continued the stateful packet cleanup around room settings, room deletion, ignored users, and catalog room/group promotion entry points by removing duplicate null branches and simplifying the surrounding Habbo/component flow.
- Continued the stateful packet sweep across figure, motto, badge, room-promotion, room-settings, gnome, and friend-furni flows by converting them to helper-first Habbo/component guards and by reducing room-state branching around active-room checks.
- Grouped the extracted room access, item, roller, and room-model files into dedicated `HabboHotel/Rooms/*` subfolders so the `Rooms` root better reflects domain boundaries after the recent service split work.
- Continued the room-entry packet cleanup across avatar movement, simple furni triggers, moderation actions, and YouTube television handlers by replacing the remaining split room guards with single helper-first Habbo room checks.
- Continued the room-bound packet sweep by collapsing repeated `InRoom` + `TryGetCurrentRoom(...)` guards in badges, rights, wired save, magic tile, branding, floorplan, gift, ignore, unignore, and mute handlers into single helper-first entry checks.
- Continued the packet cleanup across user, sound, preference, and inventory handlers by collapsing repeated `session.GetHabbo()` / nested inventory null checks into the newer pattern-based guards, including a null-safety fix for the badges inventory response path.
- Continued the packet normalization sweep across catalog, room-furni, decoration, score, hand-item, avatar-effect, and preference handlers by collapsing established Habbo/inventory/effects guards into the newer helper-first style.
- Continued the incoming packet guard sweep across avatar, friend-furni, and username-change flows by replacing direct `CurrentRoom` reads with `Habbo.TryGetCurrentRoom(...)` and by tightening established Habbo/effects guards before room-bound avatar actions run.
- Tightened another packet-handler sweep around established Habbo/session guards by normalizing respect, group management/member creation, ambassador alerts, and room connection entry points onto explicit `session.GetHabbo()` checks before continuing into room- or user-bound flows.
- Added the runtime `revisions/example.json` snapshot to version the generated header mapping alongside the codebase.
- Added a default constructor for `HabboStats` and removed the RP-specific packet/composer headers from `Resources/Revisions/1.6.6.json`.
- Upgraded `PlusEMU` and `PluginExample` from `.NET 7` to `.NET 10`.
- Updated the solution mapping from `x86` release output to `Any CPU` so Release builds now emit to `bin/Release/net10.0`.
- Adjusted `FlashOutgoingPacket` for the newer framework/compiler combination and kept the full Release build at `0 Warning(s), 0 Error(s)`.
- Collapsed `IGame` back down to its actual game-loop contract and removed the last `PlusEnvironment.Game` compatibility surface, switching shutdown/console alert flow onto directly injected managers instead of the old static game service bag.
- Replaced the last active `PlusEnvironment.GetUnixTimestamp()` / `Now()` callers in messenger and room command flows with `UnixTimestamp.GetNow()`.
- Removed the old static `PlusEnvironment.LanguageManager` surface, moving room item placement messaging and the `:update locale` flow onto injected `ILanguageManager`.
- Simplified room mute handling by resolving the target room user directly by user id instead of round-tripping through the legacy static username lookup.
- Removed the last active `PlusEnvironment.RconSocket` dependency by wiring RCON command parsing directly through the socket-owned command manager, and dropped the now-unused static `FigureManager` / `DatabaseManager` exposure from `PlusEnvironment`.
- Moved room promotion lifespan resolution off `PlusEnvironment.SettingsManager` and into the promotion purchase flow, so `RoomPromotion` no longer reaches back into the global environment for settings.
- Replaced the last active `PlusEnvironment.GetUsernameById` lookups with `ICacheManager`-backed resolution in room/group outgoing composers.
- Removed the dead in-memory `Habbo` cache tail from `PlusEnvironment`, deleting the unused `GetHabboById` / `GetHabboByUsername` helpers and the process-timer pass that walked the old static cached-user list.
- Removed the stale `[Obsolete]` marker from `RoomItemHandling` now that its old monolithic behavior has been split into dedicated item, placement, roller, persistence, tracking, and queue services and the class acts as a room-item coordinator/facade instead of the original legacy god object.
- Cleaned up stale `Item` compatibility markers by removing the dead `BaseItem` field, promoting `IsRoller` to a real computed property, and dropping obsolete tags from actively used item coordinate helpers.
- Added a first semantic helper layer to `ItemDefinition` and switched item, room-item, and catalog code over to `IsWired` / `IsTent` / `IsRoomDecoration` / `IsGroupGate` / `IsMoodlight` / `IsToner` instead of repeating the same raw `InteractionType` checks.
- Extended that `ItemDefinition` helper layer with `IsDeal`, `IsBot`, and `IsGroupFurni`, then moved another catalog, group, and room-furni guard slice off repeated raw interaction comparisons.
- Continued the same item cleanup into interaction-heavy runtime paths by routing decoration application, moodlight/toner packet guards, and item extradata generation through those newer `ItemDefinition` semantics instead of repeating hand-written interaction branches.
- Extended the item semantics into game-map and trading flows too by naming exchangeable and occupied-tile behavior on `ItemDefinition`, then reusing those predicates in trade redemption, trade payload composition, and room movement/gate checks.
- Trimmed the remaining item-tail repetition in gift purchase, room deletion, and extradata serialization by moving another small moodlight/toner/decoration slice onto the same `ItemDefinition` helper surface.
- Narrowed the older `ItemDefinition` compatibility tail a bit further by giving `WiredType` and `ExtraRot` named semantic accessors, then routing the last hot regenerate-maps/addon/random-rotation checks through those helpers.
- Added another small set of item helper predicates (`IsGift`, `IsHopper`, `IsPostIt`, `IsGnomeBox`, `IsLovelock`, `IsFloorSwitch`, `IsStickyNoteOrPhoto`) and used them to trim more raw interaction checks from packet, trade, sticky-note, and room-item service flows.
- Started carrying the same item semantics into game logic too by naming freeze tiles, team gates, football goals/counters, and banzai score items on `ItemDefinition`, then reusing those helpers in the first `GameManager` / `Freeze` / `Soccer` / `BattleBanzai` cleanup slice.
- Reduced the heavier `TeamManager` gate-update branches as well by centralizing team-count lookup, gate walk-state updates, and banzai/freeze gate resolution behind shared helpers instead of repeating the same per-color gate logic four times.
- Added a shared item-definition team resolver too, then reused it in `GameManager` and `Soccer` so banzai score updates and football gate/counter removal no longer need their own color-by-color interaction chains.
- Pushed the same item semantics deeper into `GameMap` and the Banzai tail as well by naming banzai/freeze/football special-item roles, reusing the shared team resolver for game-item registration, and trimming the last direct `Banzaifloor` checks from the scoring path.
- Trimmed another small item-special-case tail by naming bed-like and Banzai-teleport behavior on `ItemDefinition` and by routing the last `FootballGate`, `GnomeBox`, `Gift`, `Lovelock`, and `Postit` checks in `UseFurniture`, `ItemBehaviourUtility`, and `RoomUserManager` through those helpers.
- Added one more helper slice for gate/background/stacktool style edge cases too, moving `OneWayGate`, `FreezeTileBlock`, `Background`, `FxProvider`, `Stacktool`, `Gate`, and `FootballGate` checks in packets, interactors, placement, game-map, and removal code onto named `ItemDefinition` predicates.
- Trimmed another small display/composer slice too by naming pet-breeding-box, purchasable-clothing, mannequin, badge-display, monsterplant, and post-it wall-update behavior and moving those checks off raw interactions in item extradata and wall-item composers.
- Reduced another small tail around rollers, teleports, and wired item typing by moving those checks behind named `ItemDefinition` helpers and reusing them in placement validation, teleporter lookup, and `WiredComponent` trigger/effect/condition classification.
- added targeted runtime tracing for room-entry and catalog purchase handshakes to diagnose Nitro black-screen and purchase issues
- fixed avatar look update persistence/composer consistency and added runtime tracing for figure updates
- aligned Nitro 1.6.6 `GoToFlatEvent` header with the live renderer and fixed catalog offer-id mapping so room entry and purchases no longer depend on mismatched wire ids
- restored original immediate room-enter behavior after room authorization and added targeted catalog purchase abort logging for silent failure diagnosis
