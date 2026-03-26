# Legacy Refactor Status

## Purpose

This file tracks the ongoing architecture cleanup that moved packet/business logic into services and replaced legacy database wrapper usage with `DatabaseManager.Connection()` plus Dapper.

Use this file as the handoff note for later sessions.

## Current Baseline

- Project builds clean:
  - `DOTNET_ROOT=/usr/share/dotnet PATH=/usr/share/dotnet:$PATH /usr/share/dotnet/dotnet build 'Plus Emulator.csproj' -c Release --no-restore -v q`
- Last confirmed result:
  - `0 Warning(s), 0 Error(s)`
- Do not touch or commit these user-owned files unless explicitly requested:
  - `Config/config.json`
  - `CONTRIBUTION-GUIDE.txt`

## Completed Service Extractions

These packet-heavy domains were already moved into dedicated services:

- Trading
  - `HabboHotel/Rooms/Trading/TradingService.cs`
- Messenger / Friend List
  - `HabboHotel/Friends/MessengerService.cs`
- Navigator
  - `HabboHotel/Navigator/NavigatorService.cs`
- Groups
  - `HabboHotel/Groups/GroupService.cs`
- Moderation actions
  - `HabboHotel/Moderation/ModerationActionService.cs`
- Moderation tickets
  - `HabboHotel/Moderation/ModerationTicketService.cs`
- Moderation queries
  - `HabboHotel/Moderation/ModerationQueryService.cs`
- Moderation room actions
  - `HabboHotel/Moderation/ModerationRoomService.cs`
- Marketplace
  - `HabboHotel/Catalog/Marketplace/MarketplaceService.cs`
- Room rights / access
  - `HabboHotel/Rooms/RoomAccessService.cs`
- Wardrobe / clothing
  - `HabboHotel/Users/Clothing/AvatarClothingService.cs`
- Pets / bots
  - `HabboHotel/Rooms/AI/RoomCreatureService.cs`

## Completed Legacy DB Wrapper Migration

The following areas were already moved away from `GetQueryReactor()`:

- `PlusEnvironment.cs`
- `HabboHotel/Groups/Group.cs`
- `HabboHotel/Rooms/RoomUserManager.cs`
- `HabboHotel/Rooms/RoomManager.cs`
- `HabboHotel/GameClients/GameClientManager.cs`
- `HabboHotel/Users/Messenger/SearchResultFactory.cs`
- `HabboHotel/Subscriptions/SubscriptionManager.cs`
- `HabboHotel/Rooms/Chat/Commands/*`
- `HabboHotel/Users/Clothing/AvatarClothingService.cs`
- `HabboHotel/Moderation/ModerationQueryService.cs`
- `HabboHotel/Rooms/AI/RoomCreatureService.cs`
- `HabboHotel/Moderation/ModerationRoomService.cs`
- `HabboHotel/Moderation/ModerationManager.cs`
- `HabboHotel/Groups/GroupManager.cs`
- `HabboHotel/Navigator/NavigatorManager.cs`
- `HabboHotel/Navigator/NavigatorQueryService.cs`
- `HabboHotel/Items/ItemDataManager.cs`
- `HabboHotel/Games/GameDataManager.cs`
- `HabboHotel/Items/Data/Moodlight/MoodlightData.cs`
- `HabboHotel/Users/Habbo.cs`
- `HabboHotel/Rooms/Room.cs`
- `HabboHotel/Rooms/RoomFactory.cs`
- `HabboHotel/Rooms/RoomItemHandling.cs`
- `HabboHotel/Users/Effects/EffectsComponent.cs`
- `HabboHotel/Users/Effects/AvatarEffect.cs`
- `HabboHotel/Users/Effects/AvatarEffectFactory.cs`
- `HabboHotel/Users/Clothing/ClothingComponent.cs`
- `HabboHotel/Users/Calendar/CalendarComponent.cs`
- `HabboHotel/Rooms/Instance/BansComponent.cs`
- `HabboHotel/Rooms/Instance/FilterComponent.cs`
- `HabboHotel/Rooms/Chat/Pets/Locale/PetLocale.cs`

## Startup Fixes Already Applied

These were recent schema/import hardening fixes:

- `HabboHotel/Items/ItemDataManager.cs`
  - tolerant parsing for `vending_ids`
  - tolerant parsing for `height_adjustable`
  - handles `;`, empty tokens, `.25`, and malformed imported values like `1094.1089.1088.1073`
- `HabboHotel/Rooms/Chat/Pets/Locale/PetLocale.cs`
  - fixed DB column mapping to:
    - `SELECT pet_id AS Key, responses AS Value FROM bots_pet_responses`
- `HabboHotel/Permissions/PermissionManager.cs`
  - fixed `permissions_groups.badge_code` mapping

## Important Notes

- `figuremap.xml` is not used by the emulator codebase.
- `Config/figuredata.xml` is used by:
  - `Core/FigureData/FigureDataManager.cs`
  - avatar update / wardrobe / login validation flows

## Remaining Refactor Direction

The broad service-first migration is in good shape. The main remaining technical debt is the legacy wrapper and global access that still exists in older manager, helper, packet, and entity classes.

### Next Good Targets

Priority order for future sessions:

1. `HabboHotel/Catalog/Vouchers/VoucherManager.cs`
2. `HabboHotel/Catalog/Marketplace/MarketplaceManager.cs`
3. Remaining legacy instances in components like `ItemLoader`, `PetLoader` or `BotLoader`.

### Lower-Risk Short Batches

If you want easy follow-up commits, prefer small helper/manager migration batches such as:

- `AvatarEffectFactory`
- `PetLocale`
- `CalendarComponent`
- `ClothingComponent`
- `FilterComponent`
- `BansComponent`

These are already done and are good examples of the preferred migration style.

## Working Rules For Future Sessions

- Keep packet handlers thin.
- Prefer service extraction when business logic is spread across many packet files.
- Prefer `DatabaseManager.Connection()` and Dapper for new/refactored DB code.
- Avoid introducing new `GetQueryReactor()` usage.
- Avoid introducing new `PlusEnvironment.*` static dependencies when a manager/service can be injected instead.
- Build after each batch.
- Commit only the files related to the batch.
- Do not include `Config/config.json` or `CONTRIBUTION-GUIDE.txt` unless explicitly requested.

## Useful Commands

Build:

```bash
DOTNET_ROOT=/usr/share/dotnet PATH=/usr/share/dotnet:$PATH /usr/share/dotnet/dotnet build 'Plus Emulator.csproj' -c Release --no-restore -v q
```

Find remaining legacy DB wrapper usage:

```bash
rg -n "GetQueryReactor\\(" --glob '!bin/**' --glob '!obj/**'
```

Find remaining global legacy lookups:

```bash
rg -n "PlusEnvironment\\.(GetHabboById|GetUsernameById|Game|DatabaseManager)" --glob '!bin/**' --glob '!obj/**'
```

## Session Resume Suggestion

If resuming later, start with:

1. run the two `rg` commands above
2. pick one manager/entity cluster
3. migrate that batch to `Connection()`/Dapper
4. build
5. commit

