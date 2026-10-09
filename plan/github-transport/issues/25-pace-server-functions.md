# 25 — Pace server functions

**Status:** `defined`
**Type:** coding
**Blocked by:** [24 — Load and Save on the events list](24-load-save-on-events.md)
**Trial step:** 6. Next: [27 — Field test the Load trial sequence](27-field-test-the-load-trial-sequence.md).

**Binding arch:** [github-transport architecture](../arch.md), [core-refinement architecture](../../core-refinement/arch.md), [parse-thread architecture](../../parse-thread/arch.md).

## Context

Trial step 6. Server functions yield. The parse stack and the persist stack are in that set. Azure App Service Free plan is the budget: at most 60% CPU over a short window, and at most 5% CPU as a daily average. The daily average today is about 1%.

The life Workspace git Load spent 3.61 CPU minutes against a 3 CPU minute quota. One node per slice, then a yield, keeps a workspace walk inside the short window.

`requeueOnUnparsed` stays false. Turning it back on is the re-enable ticket on [parse-thread](../../parse-thread/project.md). This ticket does not write that ticket and does not flip the flag.

## Current state

1. **Parse loop** — [ParseThread.fs](../../../src/Server/ParseThread.fs) `loop` (line 102) takes one id and runs `parseOne` with no yield between items.
2. **Persist loop** — [PersistThread.fs](../../../src/Server/Core/PersistThread.fs) `loop` (line 118) calls `runOne` with no yield between items.
3. **Flag** — `requeueOnUnparsed` is false in [ParseThread.fs](../../../src/Server/ParseThread.fs). Unparsed is still set. Those ids are not pushed by the child requeue.

## What to build

Each server walk works one small slice, then yields. The two loops are the first call sites. A slice is one node. The yield is a sleep long enough that a burst of nodes stays under 60% CPU for the short window.

1. [ ] Parse yield — `loop` sleeps after each `parseOne`. The next id runs after that sleep.
2. [ ] Persist yield — `loop` sleeps after each `runOne`. The next job runs after that sleep.
3. [ ] Other hot loops — Any other server function that walks a workspace in one turn takes the same slice and sleep. Name each site in the change.
4. [ ] Budgets — The short window stays under 60% CPU. The daily average stays under 5% CPU.
5. [ ] Flag stays off — Do not set `requeueOnUnparsed` to true.
6. [ ] Test — A parse stack with two ids sleeps between them. A persist stack with two jobs sleeps between them. `requeueOnUnparsed` is still false.
