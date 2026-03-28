# Changelog

### [0.8.2] - 2026-03-28
#### Added
- `WiredExecutionData` to carry queued Wired trigger invocations through the room cycle.

#### Changed
- Moved non-chat `WiredComponent.TriggerEvent` execution onto a bounded per-room queue processed during `WiredComponent.OnCycle()`.
- Extended the queue-based Wired execution slice to cover matched `TriggerUserSays` and `TriggerUserSaysCommand` boxes without breaking the existing synchronous suppression semantics.
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

- Added the runtime `revisions/example.json` snapshot to version the generated header mapping alongside the codebase.
- Added a default constructor for `HabboStats` and removed the RP-specific packet/composer headers from `Resources/Revisions/1.6.6.json`.
- Upgraded `Plus Emulator` and `PluginExample` from `.NET 7` to `.NET 10`.
- Updated the solution mapping from `x86` release output to `Any CPU` so Release builds now emit to `bin/Release/net10.0`.
- Adjusted `FlashOutgoingPacket` for the newer framework/compiler combination and kept the full Release build at `0 Warning(s), 0 Error(s)`.
