# Legacy Refactor Status

## Purpose

This file tracks the ongoing architecture cleanup that moved packet/business logic into services and replaced legacy database wrapper usage with `DatabaseManager.Connection()` plus Dapper.

## Current Baseline

- Project builds clean:
  - `DOTNET_ROOT=/usr/share/dotnet PATH=/usr/share/dotnet:$PATH /usr/share/dotnet/dotnet build 'Plus Emulator.csproj' -c Release --no-restore -v q`
- Last confirmed result:
  - `0 Warning(s), 0 Error(s)`
- Do not touch or commit these user-owned files unless explicitly requested:
  - `Config/config.json`
  - `CONTRIBUTION-GUIDE.txt`

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

## Completed Legacy DB Wrapper Migration

All files below have been migrated off `GetQueryReactor()`.

### Core / Infrastructure
- `PlusEnvironment.cs`
- `Core/ServerStatusUpdater.cs`

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
- `HabboHotel/Users/Effects/AvatarEffectFactory.cs`
- `HabboHotel/Users/Clothing/ClothingComponent.cs`
- `HabboHotel/Users/Clothing/AvatarClothingService.cs`
- `HabboHotel/Users/Calendar/CalendarComponent.cs`

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
- `HabboHotel/Rooms/Chat/Pets/Locale/PetLocale.cs`

### Items
- `HabboHotel/Items/ItemDataManager.cs`
- `HabboHotel/Items/ItemFactory.cs`
- `HabboHotel/Items/ItemLoader.cs`
- `HabboHotel/Items/ItemTeleporterFinder.cs`
- `HabboHotel/Items/ItemHopperFinder.cs`
- `HabboHotel/Items/Interactor/InteractorHopper.cs`
- `HabboHotel/Items/Interactor/InteractorMannequin.cs`
- `HabboHotel/Items/Data/Moodlight/MoodlightData.cs`
- `HabboHotel/Items/Data/Toner/TonerData.cs`
- `HabboHotel/Items/Wired/Boxes/Effects/BotChangesClothesBox.cs`

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
- `HabboHotel/Talents/TalentTrackManager.cs`
- `HabboHotel/Talents/TalentTrackLevel.cs`
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

### Packet Handlers
- `Communication/Packets/Outgoing/Marketplace/MarketPlaceOwnOffersComposer.cs`
- `Communication/Packets/Incoming/Catalog/RedeemVoucherEvent.cs`
- `Communication/Packets/Incoming/FriendFurni/FriendFurniConfirmLockEvent.cs`
- `Communication/Packets/Incoming/Marketplace/GetMarketplaceItemStatsEvent.cs`
- `Communication/Packets/Incoming/Quests/CancelQuestEvent.cs`
- `Communication/Packets/Incoming/Quests/StartQuestEvent.cs`
- `Communication/Packets/Incoming/Quests/GetCurrentQuestEvent.cs`
- `Communication/Packets/Incoming/Rooms/Action/GiveRoomScoreEvent.cs`
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

## Startup Fixes Already Applied

- `HabboHotel/Items/ItemDataManager.cs` — tolerant parsing for `vending_ids`, `height_adjustable`; handles `;`, empty tokens, `.25`, malformed values.
- `HabboHotel/Rooms/Chat/Pets/Locale/PetLocale.cs` — fixed column mapping: `SELECT pet_id AS Key, responses AS Value FROM bots_pet_responses`
- `HabboHotel/Permissions/PermissionManager.cs` — fixed `permissions_groups.badge_code` mapping.

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
DOTNET_ROOT=/usr/share/dotnet PATH=/usr/share/dotnet:$PATH /usr/share/dotnet/dotnet build 'Plus Emulator.csproj' -c Release --no-restore -v q
```

Verify no remaining legacy usage (should return empty):

```bash
grep -r "GetQueryReactor" --include="*.cs" -l | grep -v "Database/"
```
