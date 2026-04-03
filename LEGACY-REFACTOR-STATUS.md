# Legacy Refactor Status

## Purpose

This file tracks the ongoing architecture cleanup that moved packet/business logic into services and replaced legacy database wrapper usage with `DatabaseManager.Connection()` plus Dapper.

## Current Baseline

- Project builds:
  - `DOTNET_ROOT=/usr/share/dotnet PATH=/usr/share/dotnet:$PATH DOTNET_CLI_HOME=/tmp DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 /usr/share/dotnet/dotnet build 'PlusEMU.csproj' -c Release --no-restore -v q`
- Last confirmed result:
  - `0 Warning(s), 0 Error(s)`
- Known remaining warnings:
  - none
- Do not touch or commit these user-owned files unless explicitly requested:
  - `Config/config.json`
  - `CONTRIBUTION-GUIDE.txt`

## Active In-Progress Batch

The current `master` head also contains an unfinished room / habbo lifecycle batch that is compiling cleanly but should still be treated as active refactor work rather than completed migration.

- Disconnect flow is being rerouted through `TcpSessionProxy` / `WsSessionProxy` and instance-backed `Habbo.OnDisconnect()`.
- Disconnect cleanup is now also converging back on `RoomService`, so active-room detachment during logout no longer depends on `Habbo.Dispose()` directly mutating room membership.
- Movement debugging is also tighter now: move-packet ignores, `MoveTo` rejections, no-path recalculations, and invalid-step stops now emit targeted logs, while `RoomUser` retries room client-manager lookup before movement helpers treat a human avatar as clientless.
- `RoomFactory`, `RoomManager`, `Room`, and related packet handlers are being reshaped to pass `IDatabase` and managers explicitly instead of leaning on older static/global access paths.
- `RoomManager` now owns room idle/promotion/unload lifecycle decisions, while `Room` is narrowed toward active-room cycle execution.
- `Room` constructor bootstrap is now separated from component and content initialization. Component initialization (`Gamemap`, `RoomItemHandling`, `RoomUserManager`, etc.) and content loading (`LoadFurniture`, `GenerateMaps`, `LoadPromotions`, `LoadRights`, `LoadFilter`, `InitBots`, `InitPets`) are now encapsulated in an explicit `Init()` method.
- `RoomManager` now explicitly coordinates the creation and initialization of `Room` instances by calling `Init()` post-construction, ensuring clear manager-driven lifecycle phases.
- That lifecycle split is now stricter too: `RoomManager` marks and unloads rooms, evicts active users before disposal, and re-evaluates unload eligibility after room occupancy changes, while `Room.Dispose()` itself is reduced toward resource cleanup only.
- That room idle path now keys off real-user occupancy again, so bot/pet-only rooms no longer keep reseting inactivity counters forever.
- `Room` constructor bootstrap is now grouped behind an explicit room-content initialization step, so dependency assignment is no longer interleaved with furniture/map/promotions/rights/filter/bot/pet loading.
- That room bootstrap is now further split into room-state versus creature initialization, and the bot/pet/rights/filter query-to-model translation work now sits behind dedicated helpers instead of inline deployment loops.
- `Room.ProcessRoom()` is now split into explicit active-cycle phases with a shared phase guard, so room item, user, status, game-item, and Wired ticks are no longer repeated inline try/catch branches.
- `RoomManager` room load/cycle paths are also split into explicit get/create/register/dispose and per-room cycle helpers, making the lifecycle ownership boundary clearer before any service extraction.
- That creation path is also more explicit now: `RoomManager` coordinates cache/register concerns while `RoomFactory` owns dedicated room-row query and mapping helpers instead of repeating the same projection SQL inline.
- `Room` crash/dispose teardown is now separated into user eviction, process-task disposal, collection reset, disposable system cleanup, and component cleanup phases, so room shutdown is no longer one long mixed-responsibility branch.
- Room teardown now also clears attached human habbo room references before room-user disposal runs, reducing stale disposed-room references during later disconnect handling.
- `RoomUserManager.RemoveUserFromRoom()` is now split into explicit client notification, habbo room-state reset, horse/team/trade cleanup, persistence, messenger notification, and disposal phases, so leave/disconnect handling is easier to reason about before any broader disconnect service extraction.
- `RoomService` now splits room transfer and room-entry authorization into explicit helper phases, reducing duplicate leave-current-room behavior and making the public/private/doorbell/password decision path easier to follow before the wider disconnect/session cleanup moves further outward.
- `RoomService` now also owns room-entry finalization failure handling, so avatar attach failures no longer bypass the shared leave boundary with direct `RoomUserManager` calls from room-entry packet handlers.
- Disconnect ownership is also narrower now: session proxies no longer raise logout work separately, and the server-side disconnect path now goes through an idempotent `GameClient.OnDisconnected()` that detaches the attached habbo client reference before downstream logout tasks run.
- That disconnect path is now also fail-safe against persistence errors: habbo-state saving is isolated behind guarded logging, and final disposal still runs from `finally`.
- Flow observability is also higher now: room prepare/authorize/enter, room-user add/remove, room unload/dispose, and habbo attach/detach/disconnect transitions now emit targeted logs so lifecycle ordering regressions are easier to trace from console output.
- Authentication/login lifecycle is narrower too: `Authenticator` now has explicit habbo creation, disconnect hook-up, session binding, and post-login task phases instead of wiring logout events, session registration, and login fanout inline in one method.
- Login runtime bootstrap is narrower too: manual `new EffectsComponent()`, `new ClothingComponent()`, and loose `Habbo.InitProcess(..., object, object)` wiring have been replaced by `IHabboRuntimeInitializer`, so session visual/process bootstrap now goes through one DI-owned boundary with typed process attachment.
- Service-location cleanup is narrower too: cache maintenance now takes `ICacheManager` directly, while room and command hot paths no longer depend on raw `IServiceProvider` in `RoomFactory`, `RoomManager`, `ChatManager`, or `RconSocket`, and instead route those late-bound runtime dependencies through dedicated resolver/accessor services.
- Packet/runtime activation cleanup is narrower too: `PacketManager` no longer reaches into the container directly for incoming handler activation, and `Program` no longer keeps the root service provider around just to locate shutdown services during unhandled exceptions.
- Catalog purchase nullability cleanup is in too: badge grant authorization now treats missing permission state defensively, closing the last known `CatalogService` nullability warning and restoring a clean warning-free build baseline.
- Small hardening cleanup is in too: `Program` now reuses a typed console command handler instead of repeatedly resolving it from the root container, and the user process timer no longer swallows exceptions silently or relies on null-forgiving access for its core runtime dependencies.
- Another low-risk hardening pass is in too: `Gamemap` no longer uses exception-driven door-tile assignment, and several room/client management paths now log previously silent failures during bot enumeration, user-effect application, moderator alert delivery, and shutdown persistence.
- Small command/parse cleanup is in too: sit/lay command paths no longer depend on swallowed dictionary-add exceptions, and floor-plan door height plus bot beverage parsing now use explicit bounds/parse guards instead of exception-driven fallback.
- Cache and Wired guard cleanup is in too: the cache timer now logs disposal failures and enforces its overlap guard deterministically, while the `FurniMatchStateAndPosition` condition no longer wraps direct state/rotation/position comparisons in empty catch blocks.
- Item-definition loading is a bit safer too: furniture-row mapping failures now go through structured logging instead of blocking the process on console I/O, so malformed content rows are observable without stalling startup in headless runs.
- Another small nullable cleanup is in too: `QuestManager.GetNextQuestInSeries()` now reports missing follow-up quests honestly with a nullable return, and `Gamemap.GetHighestItemForSquare()` now uses a nullable out contract instead of seeding the failure path with `null!`.
- A medium-risk guard pass is in too: group and navigator packet/service flows now treat missing room/group/category lookup results explicitly before composing responses or dereferencing access-control metadata, rather than relying on optimistic assumptions after `TryGet...` calls.
- The remaining low-risk helper cleanup is effectively exhausted too: item lookup and loaded-item helper surfaces now return nullable values on real miss paths instead of manufacturing `null!`, which closes the last small-scope helper-contract cleanup that did not require broad caller surgery.
- Packet-level room exit callers are also starting to converge on `RoomService`: hotel-view exit and username-change room reset now use `LeaveRoom()` instead of calling the room-user removal path directly.
- That shared room-exit boundary now also covers the silent room-entry failure path, since `RoomService.LeaveRoom()` takes the notify flag and `GetRoomEntryDataEvent` no longer bypasses it with its own direct removal call.
- Moderation-driven room exits are converging there as well: moderator room-kick flows now use `RoomService.LeaveRoom()` instead of calling `RoomUserManager.RemoveUserFromRoom()` directly from moderation services and commands.
- Kick-style exits are converging there too: `RoomService.KickFromRoom()` now covers the built-in room kick packet, crash-driven room eviction, and cannonball forced exits, reducing the remaining direct `RemoveUserFromRoom()` call sites in room-facing code.
- The remaining standalone moderation kick action now uses the same `RoomService` boundary too, leaving only `RoomUserManager`'s own internal lifecycle policing logic on direct removal calls.
- Packet observability is also better now: `GameClient` logs unknown incoming packet headers and packet-handler exceptions with revision/build/session/user context instead of dropping both cases silently.
- A broader `Habbo` cleanup has started too: client attach/detach and room enter/leave state now sit behind explicit `Habbo` methods, and several hot consumers no longer reach directly into raw `Client` / `CurrentRoom` fields for their core control flow.
- That `Habbo` cleanup is now spreading into room/wired command paths as well, so summon, wired kick, chat-trigger, and triggerer-on-furni checks no longer depend on raw client/room field reads in their hot path.
- Messenger follow, chat, and trading hot paths are also moving onto the same helper surface, further reducing direct `CurrentRoom` reads in user-facing service code.
- Incoming packet handlers have started moving the same direction as well: room-bound handlers are being normalized around `Habbo.TryGetCurrentRoom()` and the shared `RoomPacketEvent` room guard instead of duplicating raw `CurrentRoom` checks inline.
- That packet sweep now covers more than just avatar/action handlers too: users, catalog, wired-config, gift-open, room-settings, and floorplan packet paths are also moving onto the same active-room helper surface.
- Furni/action/inventory packet paths are joining that same migration too, shrinking another batch of repeated room guards around gate/dice/guild-furni/badge/effect handlers.
- Room access operations and YouTube television packet guards now also use the same active-room helper pattern, so another pocket of duplicated session-room lookup logic is gone.
- Follow-up cleanup is landing on top of that sweep too: once a packet handler already has an active habbo/room, extra nullable branches are being removed and kick/room-entry handlers are being tightened around the same helper boundary.
- `ItemDefinition` is also carrying more named interaction semantics now, and the last hot roller/wired-role/gift/dice/horse-equipment checks are being moved off raw `InteractionType` comparisons in placement, teleporter, gift, creature, and Wired runtime paths.
- That item-semantics sweep now also covers room-decoration and television serialization paths, so `ItemBehaviourUtility` is starting to depend on named item-role helpers instead of raw display interaction switches.
- `GameMap` special floor-effect handling is joining that same sweep too, with pool/skates style effect IDs now named on `ItemDefinition` instead of being hard-coded in the tile rebuild path.
- Team-gate and room-user game-item flow are joining that same item-semantics cleanup too, so banzai/freeze gate detection plus arrow/effect item role checks now ride named `ItemDefinition` helpers instead of duplicated interaction branches.
- Wall-item serialization is being tightened too, with post-it payload trimming now centralized behind one helper instead of being repeated in both item utility and outgoing composer code.
- `Item` itself is now starting to consume the same semantic helper surface too, with interactor selection and the first runtime update branches moving off raw interaction-case duplication and onto named item-role predicates.
- That `Item` runtime cleanup now also covers the teleporter/hopper/gate-vip/banzai slice, shrinking another dense pocket of raw interaction cases and moving banzai pulse-state mapping behind a named helper.
- `Item.ProcessUpdates()` itself is starting to be broken apart too: the hopper and teleporter branches now sit behind dedicated helper methods instead of living inline inside the main runtime switch.
- That `Item.ProcessUpdates()` cleanup now covers gate and timer branches too, with one-way gate, VIP gate, scoreboard, soccer counter, and freeze timer logic extracted into dedicated helpers plus one shared legacy-second parser.
- The cannon/forced-kick branch has also been isolated from `Item.ProcessUpdates()`, including its target-square calculation, further reducing the last dense runtime switch in the item domain.
- The old `ICatalogManager` compatibility bag is being retired too: clothing and voucher consumers are moving onto direct DI so catalog-adjacent code no longer depends on deprecated manager passthrough properties.
- `PlusEnvironment` is losing more of its legacy static shim surface too: the old enum/time/username helper methods are now gone because there are no active callers left after the recent environment and user-data cleanup.
- Some stale-but-still-compiled compatibility markers are being corrected too: dead `PingCount`, `MessengerBuddy.Client`, and no-op permission refresh paths are gone, and the still-used `MachineId` session field is treated as live state instead of a fake deprecation target.
- `PlusEnvironment` is being narrowed internally as well: services only used during startup are moving off private static fields and onto instance state, reducing how much of the emulator bootstrap still behaves like a global singleton.
- The old environment passthrough constants are shrinking too: packet decoding and numeric parsing no longer go through `PlusEnvironment` for default encoding or invariant culture, so that global helper surface is smaller again.
- Shutdown ownership is tighter now too: the emulator no longer only stops the Flash listener on shutdown, it also explicitly stops the Nitro websocket listener and RCON socket instead of relying on process exit for those listeners to disappear, and it now disposes the server-status updater timer as part of the same shutdown path.
- Runtime status reporting is tighter too: `server_status` is no longer only refreshed on a coarse timer, because connect/disconnect and room load/unload transitions now mark the status updater dirty for a near-real-time one-second flush while a slower reconciliation write remains in place as a fallback.
- The remaining active `PlusEnvironment` runtime-control surface is gone too: shutdown handling, console alerts, and startup-time tracking now resolve through dedicated runtime-control and runtime-state services instead of static environment methods/fields.
- The `Item` runtime tail is shrinking further too: more of `Item.ProcessUpdates()` now lives behind named helper methods for vending, shuffle/spin/random item states, banzai pulse/counter behavior, freeze tile activation, pressure-pad reset, and wired reset flow instead of one oversized remaining switch body.
- That item runtime cleanup now also has its own file boundary: the extracted update handlers live in `HabboHotel/Items/Runtime/Item.RuntimeUpdates.cs`, leaving `Item.cs` closer to the core item model/orchestration surface and pushing runtime behavior into a dedicated partial file.
- `RoomItemHandling` has started to be split into smaller load/remove helpers, but the broader room item lifecycle and roller/update logic is still legacy-heavy.
- The first true `RoomItemHandling` extraction is now in place too: moved-item persistence lives in `RoomItemPersistenceService`, reducing direct database-write ownership inside the room item lifecycle class.
- A second extraction is in place as well: floor-item placement and `CheckPosItem` validation now live in `RoomItemPlacementValidatorService`, pulling tile/user/stack/height rule evaluation out of `RoomItemHandling`.
- Floor and wall placement persistence now also live in `RoomItemPlacementPersistenceService`, so placement application no longer owns its own room/coordinate/wall-pos database writes.
- Roller target-state analysis plus item/user move eligibility now live in `RoomRollerService`, further shrinking direct movement-rule ownership inside `RoomItemHandling`.
- Owned-item removal ownership checks plus inventory re-add behavior now live in `RoomItemInventoryService`, further reducing `RoomItemHandling` responsibility during bulk pickup and inventory return.
- Queued item dequeue/process/requeue handling now lives in `RoomItemUpdateQueueService`, further shrinking `RoomItemHandling.OnCycle()` toward orchestration instead of raw queue mechanics.
- Furniture load normalization/recovery now lives in `RoomItemLoadService`, and room-side item removal preparation/broadcast behavior now lives in `RoomItemRemovalService`, further narrowing `RoomItemHandling` toward coordination.
- Loaded item state initialization now also lives in `RoomItemStateService`, moving roller/hopper flags plus moodlight/toner/wired startup behavior out of direct `RoomItemHandling` ownership.
- Floor/wall placement apply orchestration now also lives in `RoomItemPlacementApplyService`, so placement-side packet/map/tent/user-status/persistence coordination is no longer directly embedded in `RoomItemHandling`.
- Loaded-item registration/lookup/removal plus moved-item/roller tracking and disposal cleanup now also live in `RoomItemTrackingService`, leaving less raw collection bookkeeping inside `RoomItemHandling`.
- Roller item/user movement apply and roller-triggered Wired fanout now also live in `RoomRollerApplyService`, further shrinking the last behavior-heavy orchestration remaining inside `RoomItemHandling`.
- `RoomItemHandling.SetFloorItem` now has separate placement validation, stack-height resolution, and state-apply steps, reducing one of the largest remaining monolithic room item branches.
- `RoomItemHandling.SaveFurniture` now has explicit moved-item persistence helpers, `CycleRollers` is split into target-state plus item/user move helpers, `OnCycle` now isolates queue processing, `RemoveItems` is decomposed, `CheckPosItem` is helper-based, `SetWallItem` / disposal flow are split, and the remaining small utility branches are also helperized. The bigger remaining concern is no longer monolithic method size, but the class boundary itself.
- That cleanup line has now crossed the old threshold too: `RoomItemHandling` is no longer marked obsolete, because its original god-object behavior has been split behind dedicated services and the remaining class now primarily coordinates room-item workflows.
- Naming/grouping cleanup is also in place now: shared wall-position defaults, loaded-item lookup, and per-item floor initialization no longer repeat tiny ad hoc patterns.
- The active Wired runtime now includes room-local queue observability in `WiredComponent`, so enqueue, batch processing, saturation, and slow-cycle behavior can be traced through the standard logger pipeline while the broader room/habbo cleanup continues.
- Local emulator startup no longer silently wedges in dependency resolution either: the remaining eager singleton traps around room/chat/runtime boot have been pushed behind lazy `IServiceProvider` lookups in `RoomFactory`, `RoomManager`, `RconSocket`, `ProcessComponent`, and `ChatManager`, so local boot now reaches the real runtime initialization path instead of hanging before the banner.
- Startup failure handling is also cleaner in non-interactive runs now: `PlusEnvironment` no longer throws a second `Console.ReadKey` exception when the database check fails under redirected input, so smoke tests report the actual MySQL connectivity problem and stop there.
- Room-model bootstrapping is aligned with the live SQL schema now too: `RoomManager` maps `room_models.club_only` / `wall_height` rows into `RoomModel` explicitly instead of relying on a stale `public_room` projection and Dapper constructor matching.
- Login/logout cleanup is aligned with the live `users` schema now too: SSO reset paths clear `auth_ticket` with an empty string instead of `NULL`, matching the non-null column constraint used by the shipped SQL dumps.
- Optional seasonal user-data loading is more robust now too: missing `user_xmas15_calendar` tables are treated as absent legacy content instead of fatal login blockers, so SSO authentication can continue with an empty calendar state.

## Migration Status: ✅ COMPLETE

All active `GetQueryReactor()` usages have been eliminated and replaced with `DatabaseManager.Connection()` + Dapper.

Remaining references to `GetQueryReactor` in the codebase:
- `Database/Database.cs` — the implementation itself, marked `[Obsolete]`, kept for interface compatibility.
- `Database/IDatabase.cs` — the interface declaration, marked `[Obsolete]`.
- `Database/DatabaseConnection.cs` — the legacy adapter internals.
- `HabboHotel/Users/Messenger/HabboMessengerOld.cs` — entirely commented out, no active code.

## Completed Service Extractions

These packet-heavy domains were already moved into dedicated services:

- Trading — `HabboHotel/Rooms/Trading/TradingService.cs`
- Messenger / Friend List — `HabboHotel/Friends/MessengerService.cs`
- Navigator — `HabboHotel/Navigator/NavigatorService.cs`
- Groups — `HabboHotel/Groups/GroupService.cs`
- Moderation actions — `HabboHotel/Moderation/ModerationActionService.cs`
- Moderation tickets — `HabboHotel/Moderation/ModerationTicketService.cs`
- Moderation queries — `HabboHotel/Moderation/ModerationQueryService.cs`
- Moderation room actions — `HabboHotel/Moderation/ModerationRoomService.cs`
- Marketplace — `HabboHotel/Catalog/Marketplace/MarketplaceService.cs`
- Room rights / access — `HabboHotel/Rooms/RoomAccessService.cs`
- Wardrobe / clothing — `HabboHotel/Users/Clothing/AvatarClothingService.cs`
- Pets / bots — `HabboHotel/Rooms/AI/RoomCreatureService.cs`
- Catalog — `HabboHotel/Catalog/CatalogService.cs`
- Quests — `HabboHotel/Quests/QuestService.cs`
- Achievements — `HabboHotel/Achievements/AchievementService.cs` (Phase 1, 2, 3 complete)
- Chat — `HabboHotel/Rooms/Chat/ChatService.cs` (Phase 4 complete)
- Rooms — `HabboHotel/Rooms/RoomService.cs`
    *   [x] Phase 5: Room Modernization & DI (RoomService, async commands).
    *   [x] Phase 6: Item/Furniture Modernization (IItemService, IItemLoader, Dapper).
    *   [x] Phase 7: Catalog Modernization (ICatalogService, IBotUtility, IPetUtility, Dapper).
    *   [x] Phase 8: Navigator & Room System Modernization (IRoomFactory, IRoomAppender, DI).
    *   [x] Phase 9a: Dependency Injection Modernization (Outgoing Packet Composers & Incoming Event Handlers).

## Completed Legacy DB Wrapper Migration

All files below have been migrated off `GetQueryReactor()`.

### Core / Infrastructure
- `PlusEnvironment.cs`
- `Core/ServerStatusUpdater.cs`
- `HabboHotel/Items/ItemFactory.cs` (Phase 6 complete)
- `HabboHotel/Items/ItemLoader.cs` (Phase 6 complete)

### GameClients / Users
- `HabboHotel/GameClients/GameClientManager.cs`
- `HabboHotel/Users/Habbo.cs`
- `HabboHotel/Users/Process/ProcessComponent.cs`
- `HabboHotel/Users/Messenger/SearchResultFactory.cs`
- `HabboHotel/Users/Inventory/Bots/BotLoader.cs`
- `HabboHotel/Users/Inventory/Pets/PetLoader.cs`
- `HabboHotel/Subscriptions/SubscriptionManager.cs`
- `HabboHotel/Users/Effects/EffectsComponent.cs`
- `HabboHotel/Users/Effects/AvatarEffect.cs`
- `HabboHotel/Users/Clothing/ClothingComponent.cs`
- `HabboHotel/Users/Clothing/AvatarClothingService.cs`
- `HabboHotel/Users/Calendar/CalendarComponent.cs` (Modernized + DI)
- `HabboHotel/Users/Calendar/LoadUserCalendarTask.cs` (New)

### Rooms
- `HabboHotel/Rooms/RoomManager.cs`
- `HabboHotel/Rooms/RoomFactory.cs`
- `HabboHotel/Rooms/Room.cs`
- `HabboHotel/Rooms/RoomUserManager.cs`
- `HabboHotel/Rooms/RoomItemHandling.cs`
- `HabboHotel/Rooms/Instance/BansComponent.cs`
- `HabboHotel/Rooms/Instance/FilterComponent.cs`
- `HabboHotel/Rooms/Instance/WiredComponent.cs`
- `HabboHotel/Rooms/AI/RoomCreatureService.cs`
- `HabboHotel/Rooms/Chat/Commands/*` (all user, fun, mod, admin commands)
- `HabboHotel/Rooms/Chat/Pets/Locale/PetLocale.cs` (Modernized + DI)

### Items
- `HabboHotel/Items/ItemDataManager.cs`
- `HabboHotel/Items/ItemFactory.cs`
- `HabboHotel/Items/ItemLoader.cs`
- `HabboHotel/Items/ItemTeleporterFinder.cs` (Modernized + DI)
- `HabboHotel/Items/ItemHopperFinder.cs` (Modernized + DI)
- `HabboHotel/Items/IItemTeleporterFinder.cs` (New)
- `HabboHotel/Items/IItemHopperFinder.cs` (New)
- `HabboHotel/Items/ItemBehaviourUtility.cs`
- `HabboHotel/Items/Interactor/InteractorHopper.cs`
- `HabboHotel/Items/Interactor/InteractorMannequin.cs`
- `HabboHotel/Items/Data/Moodlight/MoodlightData.cs`
- `HabboHotel/Items/Data/Toner/TonerData.cs`
- `HabboHotel/Items/Wired/Boxes/Effects/BotChangesClothesBox.cs`
- `HabboHotel/Items/Wired/Boxes/Effects/GiveUserBadgeBox.cs`

### Catalog
- `HabboHotel/Catalog/Marketplace/MarketplaceManager.cs`
- `HabboHotel/Catalog/Pets/PetRaceManager.cs`
- `HabboHotel/Catalog/Utilities/BotUtility.cs`
- `HabboHotel/Catalog/Utilities/PetUtility.cs`
- `HabboHotel/Catalog/Vouchers/Voucher.cs`
- `HabboHotel/Catalog/Vouchers/VoucherManager.cs`

### Groups / Navigator / Moderation / Permissions
- `HabboHotel/Groups/Group.cs`
- `HabboHotel/Groups/GroupManager.cs`
- `HabboHotel/Navigator/NavigatorManager.cs`
- `HabboHotel/Navigator/NavigatorQueryService.cs`
- `HabboHotel/Moderation/ModerationManager.cs`
- `HabboHotel/Moderation/ModerationQueryService.cs`
- `HabboHotel/Moderation/ModerationRoomService.cs`
- `HabboHotel/Permissions/PermissionManager.cs`
- `HabboHotel/Games/GameDataManager.cs`

### Talents / Quests / Rewards
- `HabboHotel/Talents/TalentTrackManager.cs` (Modernized + DI)
- `HabboHotel/Talents/TalentTrackLevel.cs` (Modernized + DI)
- `HabboHotel/Talents/TalentTrackSubLevel.cs`
- `HabboHotel/Quests/QuestManager.cs`
- `HabboHotel/Rewards/RewardManager.cs`

### RCON Commands
- `Communication/RCON/Commands/User/GiveUserCurrencyCommand.cs`
- `Communication/RCON/Commands/User/TakeUserCurrencyCommand.cs`
- `Communication/RCON/Commands/User/ReloadUserCurrencyCommand.cs`
- `Communication/RCON/Commands/User/SyncUserCurrencyCommand.cs`
- `Communication/RCON/Commands/User/ReloadUserMottoCommand.cs`
- `Communication/RCON/Commands/User/ReloadUserRankCommand.cs`
- `Communication/RCON/Commands/User/ReloadUserVIPRankCommand.cs`
- `Communication/RCON/Commands/Hotel/ReloadServerSettingsCommand.cs` (Modernized + DI)

### Packet Handlers
- `Communication/Packets/Outgoing/Marketplace/MarketPlaceOwnOffersComposer.cs`
- `Communication/Packets/Incoming/Catalog/RedeemVoucherEvent.cs`
- `Communication/Packets/Incoming/FriendFurni/FriendFurniConfirmLockEvent.cs`
- `Communication/Packets/Incoming/Marketplace/GetMarketplaceItemStatsEvent.cs`
- `Communication/Packets/Incoming/Quests/CancelQuestEvent.cs`
- `Communication/Packets/Incoming/Quests/StartQuestEvent.cs`
- `Communication/Packets/Incoming/Quests/GetCurrentQuestEvent.cs`
- `Communication/Packets/Incoming/Rooms/Action/GiveRoomScoreEvent.cs`
- `Communication/Packets/Incoming/Rooms/Action/AmbassadorAlertEvent.cs` (Modernized + DI)
- `Communication/Packets/Incoming/Rooms/AI/Pets/Horse/ModifyWhoCanRideHorseEvent.cs`
- `Communication/Packets/Incoming/Rooms/Avatar/ChangeMottoEvent.cs`
- `Communication/Packets/Incoming/Rooms/Engine/ApplyDecorationEvent.cs`
- `Communication/Packets/Incoming/Rooms/Engine/UseFurnitureEvent.cs`
- `Communication/Packets/Incoming/Rooms/FloorPlan/SaveFloorPlanModelEvent.cs`
- `Communication/Packets/Incoming/Rooms/Furni/CreditFurniRedeemEvent.cs`
- `Communication/Packets/Incoming/Rooms/Furni/OpenGiftEvent.cs`
- `Communication/Packets/Incoming/Rooms/Furni/SetTonerEvent.cs`
- `Communication/Packets/Incoming/Rooms/Furni/Stickys/DeleteStickyNoteEvent.cs`
- `Communication/Packets/Incoming/Rooms/Settings/DeleteRoomEvent.cs`
- `Communication/Packets/Incoming/Rooms/Settings/SaveRoomSettingsEvent.cs`
- `Communication/Packets/Incoming/Users/SetUserFocusPreferenceEvent.cs`
- `Communication/Packets/Incoming/Users/UpdateFigureDataEvent.cs`
- `Communication/Packets/Outgoing/Rooms/Engine/UsersComposer.cs`
- `Communication/Packets/Outgoing/FriendList/MessengerInitComposer.cs`
- `Communication/Packets/Outgoing/Groups/GroupCreationWindowComposer.cs`
- `Communication/Packets/Outgoing/Rooms/Settings/GetRoomBannedUsersComposer.cs`
- `Communication/Packets/Outgoing/Rooms/Settings/RoomRightsListComposer.cs`
- `Communication/Packets/Outgoing/Users/ProfileInformationComposer.cs`
- `Communication/Packets/Incoming/Rooms/Settings/GetRoomRightsEvent.cs`
- `Communication/Packets/Incoming/Groups/GetGroupCreationWindowEvent.cs`
- `Communication/Packets/Incoming/Game/Lobby/GetGameListEvent.cs`
- `Communication/Packets/Incoming/Marketplace/GetMarketplaceItemStatsEvent.cs`
- `Communication/Packets/Incoming/Rooms/AI/Pets/Horse/ModifyWhoCanRideHorseEvent.cs`
- `Communication/Packets/Incoming/Users/OpenPlayerProfileEvent.cs`

## Startup Fixes Already Applied

- `HabboHotel/Items/ItemDataManager.cs` — tolerant parsing for `vending_ids`, `height_adjustable`; handles `;`, empty tokens, `.25`, malformed values.
- `HabboHotel/Rooms/Chat/Pets/Locale/PetLocale.cs` — fixed column mapping: `SELECT pet_id AS Key, responses AS Value FROM bots_pet_responses`
- `HabboHotel/Permissions/PermissionManager.cs` — fixed `permissions_groups.badge_code` mapping.
- `HabboHotel/Users/Calendar/LoadUserCalendarTask.cs` — separated advent calendar loading from the old all-in-one user bootstrap flow.

## Important Notes

- `figuremap.xml` is not used by the emulator codebase.
- `Config/figuredata.xml` is used by `Core/FigureData/FigureDataManager.cs` and avatar update / wardrobe / login validation flows.
- `GetQueryReactor()` still exists in `IDatabase` marked `[Obsolete]`. It can be removed in a future breaking-change cleanup.

## Working Rules For Future Sessions

- Keep packet handlers thin.
- Prefer service extraction when business logic is spread across many packet files.
- Always use `DatabaseManager.Connection()` and Dapper for DB code — never `GetQueryReactor()`.
- Avoid introducing new `PlusEnvironment.*` static dependencies when a manager/service can be injected instead.
- Build after each batch.
- Commit only the files related to the batch.
- Do not include `Config/config.json` or `CONTRIBUTION-GUIDE.txt` unless explicitly requested.

## Useful Commands

Build:

```bash
DOTNET_ROOT=/usr/share/dotnet PATH=/usr/share/dotnet:$PATH /usr/share/dotnet/dotnet build 'PlusEMU.csproj' -c Release --no-restore -v q
```

Verify no remaining legacy usage (should return empty):

```bash
grep -r "GetQueryReactor" --include="*.cs" -l | grep -v "Database/"
```

```bash
grep -r "ProgressAchievement" --include="*.cs" | grep "AchievementManager" | grep -v "IAchievementManager" | grep -v "AchievementManager.cs" | grep -v "AchievementService.cs"
```

## Upcoming Architecture Tasks
- Continue moving room lifecycle/bootstrap ownership out of `Room` constructor/setup paths and into clearer manager/factory-driven phases.
- Start the first true `RoomItemHandling` service extraction once the room lifecycle ownership boundary is stable.
- Continue the incoming packet sweep by replacing ad-hoc `session.GetHabbo()` / room-null guard patterns with the newer explicit Habbo helper flow across group, users, connection, and moderation-adjacent handlers.
- Keep folding avatar and room-adjacent packet handlers onto `Habbo.TryGetCurrentRoom(...)` so username-change, furni interaction, and avatar action flows no longer read `Habbo.CurrentRoom` directly.
- Continue collapsing packet-side Habbo/inventory/effects guard boilerplate so catalog, preference, and furni handlers all follow the same helper-first flow before deeper service extraction.
- Keep normalizing simple user and inventory packet entry points onto pattern-based Habbo guards so packet handlers stop open-coding nested null checks around inventory/components.
- Continue folding room-bound packet handlers onto single-entry room resolution so `InRoom`/`TryGetCurrentRoom(...)` checks stop being duplicated across moderation, furni, floorplan, and room-settings packets.
- Continue sweeping the remaining avatar/action/furni packet tail so even small movement and trigger handlers share the same single-entry Habbo room guard style before broader service extraction.
- Keep the `HabboHotel/Rooms` physical layout aligned with the extracted architecture by grouping access, item, roller, and model files under dedicated subfolders as service boundaries stabilize.
- Continue the remaining stateful packet cleanup so profile, badge, promotion, settings, and friend-furni handlers also rely on the same Habbo helper surface instead of open-coded session/component branching.
- Keep trimming the packet tail by simplifying the heavier room settings / delete-room / ignore-list flows before moving the same Habbo helper cleanup into services and commands.
- Continue the last packet-tail cleanup by extracting repeated helperless blocks from delete-room, room-settings, and large catalog purchase handlers before shifting focus to `Habbo` usage in services/commands.
- Continue the `Habbo` helper migration in services/commands after the packet sweep, starting with `GroupService` and then the moderator/user command set that still reads `CurrentRoom` / `Client` directly.
- Keep sweeping the command layer after `GroupService`, starting with `CommandManager` and the hot moderator/user commands that still compare rooms or address clients through raw `Habbo` properties.
- Continue sweeping the remaining command tail after those first command batches, especially the leftover moderator/user commands that still reach for direct `Habbo` room/client state instead of the helper surface.
- The chat command folder no longer relies on direct `Habbo.CurrentRoom` / `Habbo.Client` reads; remaining `Habbo` helper migration work now sits outside packet and command handlers.
- The remaining `Habbo` helper migration is now focused on service/supporting-domain code such as ignores, marketplace, clothing, badges/effects, moderation, navigator, and room-side helper flows.
- That service-level migration is now reaching the heavier runtime paths too, including moderation actions/tickets, room enter/leave orchestration, trading, and process-tick notification flows.
- A smaller tail cleanup is also underway across navigator friend-room queries plus interactor/Wired effect helpers, which is shrinking the remaining direct room/client property reads around runtime convenience code.
- `RoomUserManager` is now joining that same cleanup too: the remaining avatar validation, team-gate, and teleporter-side room checks are moving onto `_room` ownership and `Habbo` helper methods instead of raw `CurrentRoom` reads.
- Team/game support code is joining it too, with `TeamManager` now using a shared room-resolution helper instead of duplicating raw `CurrentRoom` access in each gate-update branch.
- `RoomCreatureService` has also started moving the same direction: pet/bot entry points are beginning to use the owned room parameter or `Habbo.TryGetCurrentRoom(...)` instead of raw `CurrentRoom` reads.
- The remaining helper-migration tail is now mostly about removing redundant `InRoom` guards where `TryGetCurrentRoom(...)` already provides the same safety, plus finishing the last few runtime service consumers.
- Navigator friend-room lookup no longer depends on direct buddy room property reads either; the remaining `Habbo` helper work is now mostly about whether older compatibility properties like `InRoom` / `CurrentRoom` should stay exposed at all.
- That final compatibility question is now closed for these user models: `Habbo` and `MessengerBuddy` no longer expose room-state through the old public `CurrentRoom` / `InRoom` surface, and callers resolve room state through explicit helpers instead.
- The old `IGame` compatibility bag has now been cut back to lifecycle operations only. `PlusEnvironment` no longer exposes the legacy static `Game` surface, and shutdown / console alert flow now resolves through directly injected managers instead of the global service bag.
- The legacy `PlusEnvironment` surface has been reduced further: language reload/lookup now resolves through DI, and room mute handling no longer depends on the static username helper for in-room target resolution.
- The `PlusEnvironment` static surface has been narrowed again: RCON parsing no longer reaches back through the global environment, and the unused `FigureManager` / `DatabaseManager` static exposure has been removed.
- The remaining `PlusEnvironment` static surface has narrowed again: room promotion creation no longer pulls settings from the global environment, and outgoing room/group payloads no longer depend on the static username lookup helper.
- The last static `Habbo` cache tail is gone from `PlusEnvironment`: the old `GetHabboById` / `GetHabboByUsername` helpers and their timer-driven cached-user cleanup path have been removed.
- `Item` has also been trimmed a bit further: stale obsolete markers on actively used coordinate/roller helpers are gone, and the dead `BaseItem` compatibility field has been removed.
- `ItemDefinition` now carries a first batch of semantic interaction helpers too, and the surrounding item, room-item, and catalog code has started using those predicates instead of open-coding the same raw `InteractionType` comparisons.
- That `ItemDefinition` cleanup is now widening as well: deal/bot/group-furni semantics are starting to move behind named predicates instead of repeated hand-written interaction checks in catalog and group flows.
- The same item semantics are now starting to reach the more interaction-heavy paths too, including decoration application, moodlight/toner packet guards, and item extradata serialization.
- Game-map and trading code is joining that same cleanup as well: exchange and occupied-tile behavior now have named item-definition predicates instead of being repeated inline in movement and trade flows.
- A smaller item-tail cleanup is underway too around gift purchase and room teardown paths, where moodlight/toner/decoration guards are being folded into those same named predicates instead of staying hand-written.
- Even the smaller `ItemDefinition` compatibility tail is starting to get names now too: regenerate-maps wired checks, random wired addon checks, and extra-rotation placement checks are moving behind explicit semantic accessors instead of raw field reads.
- The helper surface is widening across the smaller packet/service tail as well, with sticky-note, gnome, lovelock, hopper, gift, exchange, and floor-switch checks beginning to move off repeated raw interaction comparisons.
- Game logic is starting to join that same migration too: freeze tiles/blocks, team gates, football goals/counters, and banzai score items now have named semantics that the first game-manager and minigame paths can reuse instead of repeating raw interaction checks.
- `TeamManager` is now joining that same cleanup too, with the large per-color banzai/freeze gate update branches collapsing behind shared team-count and gate-state helpers instead of staying fully duplicated.
- That game cleanup is now gaining a shared team resolver as well, so item-to-team mapping for banzai scores and football gates/counters can be reused instead of staying duplicated across each minigame path.
- `GameMap` and the remaining Banzai tail are now joining it too: special banzai/freeze/football item registration/removal logic is moving behind the same helper semantics and shared team resolver instead of spelling out each interaction inline.
- The smaller item-special-case tail is shrinking too: bed/tent, Banzai teleporter, football-gate, gnome, gift, lovelock, and sticky-note checks are being folded into `ItemDefinition` helpers instead of staying scattered across room-user, packet, and extradata code.
- Another small edge-case slice has joined that same cleanup too, with one-way gates, freeze blocks, branding backgrounds, FX providers, stacktools, generic gates, and football-gate cleanup starting to read through named `ItemDefinition` predicates instead of raw interaction comparisons.
- The display/composer tail is joining it as well: pet-breeding boxes, purchasable clothing, mannequins, badge displays, monsterplant items, and wall post-it rendering now lean on named `ItemDefinition` helpers instead of more raw interaction checks.
- Roller, teleport, and Wired item classification are now joining that same item-definition cleanup too, so placement validation, teleporter lookup, and `WiredComponent` type checks no longer need to read raw interaction values for those common paths.
- added targeted room-entry and catalog purchase runtime tracing to isolate Nitro client flow mismatches in live testing
- tightened avatar figure-update flow so filtered look data is persisted and echoed consistently, with live tracing for Nitro clothing-debug runs
- aligned the live Nitro 1.6.6 room-enter follow-up header and catalog offer-id flow with the client renderer, removing two concrete protocol mismatches behind black-screen and silent purchase failures
- corrected the room authorization flow back to original immediate-enter semantics and added targeted catalog purchase abort tracing so service extraction no longer hides silent protocol/lookup failures
