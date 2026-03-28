# PlusEMU Wired Asynchronous Execution Plan (Execution Queue Model)

## Current Implementation Status

Phase 1 is now in place:
- `WiredComponent.TriggerEvent` queues room / furni / game / command trigger invocations instead of executing them immediately on the caller thread.
- `WiredComponent.OnCycle()` drains the queued trigger invocations in bounded batches before running existing `IWiredCycle` boxes.
- `TriggerUserSays` and `TriggerUserSaysCommand` now queue only the matched trigger boxes while still returning synchronously so the legacy chat/command suppression behavior is preserved.
- The synchronous chat/command suppression decision is now isolated to `WiredComponent`, while the queued `UserSays` trigger boxes no longer re-resolve the match during execution.
- Queued chat-trigger execution now travels through a typed `WiredChatTriggerContext`, which trims one more `params object[]` hot path without forcing a full interface break yet.
- The same typed handoff pattern now also covers walk/furni/state triggers via `WiredActorItemTriggerContext`, reducing another cluster of positional queue payload unpacking.
- `TriggerRoomEnter` now uses its own typed actor context and the game start/end triggers are queued through an explicit parameterless path, leaving fewer legacy queue payload shapes in the dispatcher.
- `WiredContextResolver` now centralizes repeated actor and actor-item extraction across multiple trigger, condition, and effect boxes, reducing the amount of per-box payload decoding left before a larger interface change.
- That shared resolver now also covers more of the actor-centric triggerer/team/hand-item path, narrowing the remaining legacy `Habbo` cast sites to a much smaller tail set.
- The last actor-only effect tail (`TeleportUser`, nested stacks, badge rewards, and bot-targeted actions) is now on the same resolver path too, leaving only a small number of truly custom payload decoders behind.
- Random selected-furni resolution for teleport/move style effects is also being centralized now, which trims another repeated stateful helper pattern before any deeper API break.
- A few remaining boxes that never actually consumed trigger payloads have now dropped their fake `@params` dependency too, which makes the eventual interface split cleaner.
- Shared parsing helpers are now taking over common `StringData` decoding paths as well, reducing another chunk of duplicated condition-side payload handling before a wider API break.
- Team-targeted Wired boxes now share a small parser/helper too, continuing the same pattern of moving repeated decode logic out of individual box implementations.
- Saved furni snapshot decoding is now shared between `MatchPosition` and the related state/position condition boxes, trimming another repeated `ItemsData` parsing path.
- `WiredComponent` now also routes its legacy box invocation through a small execution adapter layer, which gives the codebase a bridge point for any future `Execute` interface split without forcing that break yet.
- The first typed execution slice is now live as well: `IWiredExecutable` plus `WiredExecutionContext` sit beside the legacy interface, and a few representative boxes already execute through that new path.
- That typed path now covers a wider actor-driven slice too, which means both trigger and effect boxes are starting to move off raw `params object[]` decoding and onto the new execution context.
- The typed path now reaches into actor-item triggers and some representative condition/effect boxes as well, which confirms the bridge can scale beyond the first narrow sample.
- The negative/paired actor conditions are now moving over too, which means the typed execution path is no longer limited to only a few hand-picked happy-path boxes.
- The same typed bridge now covers the triggerer/team/hand-item actor slice too, leaving a noticeably smaller actor-based legacy `params object[]` surface behind.
- The remaining actor-targeted effect slice is moving across that bridge as well, which means queued teleport, nested stack, and bot-targeting effects now share the same typed entry path.
- A matching parameterless/data-only slice is on that bridge too now, which broadens the new execution path beyond actor payload scenarios and reduces the amount of legacy adapter-only traffic still left.
- The same is now true for one furni-occupancy condition cluster as well, so the typed path is starting to cover non-actor conditions instead of only trigger/effect happy paths.
- The remaining user-count and state/position conditions are joining that bridge too, leaving a much smaller legacy condition surface behind than before.
- The same bridge now carries the remaining bot-targeted/data-only effect slice too, which trims another block of boxes that were still only reachable through the legacy `Execute(params object[])` path.
- The delayed/cycle-backed boxes are on that bridge now as well, which means the typed execution entry point covers not just direct trigger/effect calls but also the room-cycle scheduling path that Phase 1 introduced.
- At the contract level, `IWiredExecutable` and `WiredExecutionContext` are now the primary execution surface, while the old variadic `Execute(params object[])` signature is explicitly retained only as a compatibility bridge.
- Internally, the adapter layer now dispatches directly through typed contexts as well, so the compatibility path is no longer part of normal room-cycle or trigger-stack execution.
- The compatibility bridge itself now lives on the interface rather than in every box implementation, which lets the repetitive forwarding wrappers be removed incrementally without changing behavior.
- That incremental cleanup is now well underway too, with the larger actor/chat trigger slice already moved off per-box wrapper methods and onto the shared interface bridge.
- That wrapper cleanup is now complete for the box layer, leaving the legacy variadic path centralized in one place instead of duplicated across the individual Wired boxes.
- That last centralized compatibility method is now gone too, so the execution contract is fully typed around `WiredExecutionContext`.
- The next refinement phase has now started as well: queued chat triggers already get their own dedicated execution context, which gives the broader context-splitting work a concrete first slice.
- The actor+item queued trigger family is on that path too now, which means the first non-chat trigger cluster no longer depends on the broad generic context shape during dispatch.
- Actor-only execution is now on the same path: room-enter style queued triggers and the shared actor-driven stack/effect flow no longer need the broad generic context either.
- Parameterless execution is on a dedicated empty context now too, and the broad context has already shed some fields that were no longer read anywhere in the Wired box layer.
- The split has now reached the executable contracts as well for the first slices, so chat, actor-item, and empty execution can be dispatched through narrower interfaces instead of only the broad base one.
- Actor-only condition execution is now moving onto the same specialized contract pattern too, which narrows another large slice of the remaining broad-interface dispatch surface.
- That actor-only specialization now covers the remaining triggerer/effect tail as well, leaving very little of the normal Wired execution flow on the unspecialized base interface.
- The specialized execution contracts now also own their own `IWiredExecutable` bridge methods, which means boxes no longer need to repeat that same cast-and-forward shim individually once they adopt one of the narrower interfaces.
- Shared trigger-stack helpers in `WiredComponent` now execute the common condition / random-addon / effect flow for multiple trigger box types, reducing duplicate execution code before the larger async migration continues.
- `RepeaterBox` and `ExecuteWiredStacksBox` also use centralized `WiredComponent` execution helpers now, so the remaining migration work is concentrated more tightly around scheduling and side-effect isolation rather than duplicate traversal code.
- The delayed-cycle effect boxes are also being normalized around shared scheduling helpers, reducing per-box timing boilerplate before any larger queue/callback redesign.
- `TeleportUserBox` and `KickUserBox` have also been moved off their legacy non-generic queue handling, keeping the queued user-targeting effect boxes closer to the same typed scheduling baseline.
- `MatchPositionBox` no longer drives its state replay flow through repeated string splits and parse exceptions, which narrows one more legacy hot path before larger scheduling work continues.
- `WiredCycleScheduler` now also owns the common "mark requested / schedule next tick" helpers, tightening the remaining delayed effect boxes around the same request lifecycle.

## Abstract
The Wired system in PlusEMU relies heavily on a sequential execution tree (`Trigger` -> `Condition` -> `Effect`). Translating all synchronous `IWiredItem.Execute` methods into `Task<bool> ExecuteAsync()` natively risks severe race-conditions because the `Room` components (such as `RoomUserManager`, `GameMap`, and `RoomItemHandling`) are absolutely **NOT** thread-safe.

Parallel thread execution (`Task.Run`) for the Wired events can lead to the following side effects:
- Concurrent modification exceptions on active item grids or user positioning logic.
- Two users walking on triggers simultaneously blocking or overriding each others' state changes.

To accomplish non-blocking asynchronous behaviors safely, we will adopt the **Wired Execution Queue Model**. This guarantees that executing long chains of Wired blocks won't lock up `RoomManager` while still respecting the inherently single-threaded nature of the `OnCycle` room logic loop.

## The Problem
When a Wired trigger is fired (e.g. `UserWalksOnBox.cs`), it iterates over a list of items (`Instance.GetWired().GetEffects(this)`) and invokes `.Execute` on each of them immediately. Because they are processed on the invocation site, heavy effects that take longer to process or loop over entire room actors will effectively block the overall server/room thread sequence.

## The Solution: Execution Queue Mechanism
We will not refactor the existing 60+ `Execute` signatures immediately. Rather, we will redesign the backbone sequence dispatcher:

1. **New `WiredExecutionQueue` property on `WiredComponent`**:
   - `private readonly ConcurrentQueue<WiredExecutionData> _executionQueue;`

2. **Decoupled Invocation (`TriggerEvent`)**:
   - The `TriggerEvent` mapping will no longer immediately resolve and loop `Execute`.
   - Instead, it identifies the target triggers and enqueues the required wired chain arguments into `_executionQueue`.

3. **Room Tick Integration (`OnCycle`)**:
   - The existing `WiredComponent.OnCycle()` invoked by `Room.OnCycle()` will pull operations off the Execution Queue in batches.
   - It will perform execution recursively. This keeps the execution sequence strictly locked to the room's single ticking loop constraint, natively avoiding race conditions inside the `GameMap`.

4. **Backgrounding Isolated Operations (Dapper/DB writes)**:
   - For components natively bound by Database locks (like awarding badges/furni or logging entries), `Task.Run` blocks will be issued *inside* their isolated methods instead.

## Steps for Implementation
1. Add `WiredExecutionData` class struct wrapper.
2. Initialize `ConcurrentQueue<WiredExecutionData> _executionQueue;` within `WiredComponent.cs`.
3. Swap `TriggerEvent` direct executing logic to populate the queue.
4. Expand `WiredComponent.OnCycle()` to safely dequeue and process pending executions.
5. Identify isolated slow operations (e.g., `GiveUserBadgeBox.cs`) and push *their internal logic* to asynchronous `Task.Run` methods without updating the base `IWiredItem.Execute` signature.
