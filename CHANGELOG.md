# Changelog

## 2026-03-24

### Build Cleanup

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
