# PlusEMU Wired Asynchronous Execution Plan (Execution Queue Model)

## Current Implementation Status

Phase 1 is now in place:
- `WiredComponent.TriggerEvent` queues room / furni / game / command trigger invocations instead of executing them immediately on the caller thread.
- `WiredComponent.OnCycle()` drains the queued trigger invocations in bounded batches before running existing `IWiredCycle` boxes.

The chat-trigger path (`TriggerUserSays`) still executes synchronously for now so the legacy "consume chat when the trigger fires" behavior is preserved. The remaining trigger types can be migrated once their caller-side return-value semantics are no longer coupled to immediate execution.

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
