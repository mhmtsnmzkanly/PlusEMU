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
