# 21 — Git Load posts the Workspace

**Status:** `defined`
**Type:** coding
**Blocked by:** [23 — Directory or File Load posts to the parse stack](23-directory-file-load-parse-stack.md)
**Trial step:** 3. Next: [22 — Drop the Browser git Load after-step](22-drop-browser-git-load-after-step.md).

**Binding arch:** [github-transport architecture](../arch.md), [core-refinement architecture](../../core-refinement/arch.md), [parse-thread architecture](../../parse-thread/arch.md).

## Context

Trial step 3. `runLoad` stops calling `reconcileWorkspace` inside the Actor. It posts the Workspace to the parse stack.

The post is that one Workspace node. Desk Directory Load and desk File Load post their own nodes in [23 — Directory or File Load posts to the parse stack](23-directory-file-load-parse-stack.md). This ticket leaves those desk posts as they are.

`requeueOnUnparsed` stays false. Pacing is [25 — Pace server functions](25-pace-server-functions.md). The Browser after-step stays until [22 — Drop the Browser git Load after-step](22-drop-browser-git-load-after-step.md).

## Current state

1. **Actor** — [GithubTransportActor.fs](../../../src/Server/GithubTransportActor.fs) `runLoad` (line 120) pulls, then calls `dependencies.continueLoad`, then `parseFocusFile`.
2. **Inline reconcile** — `productionDependencies` (line 216) sets `continueLoad` to [LazyLoadReconciliationServer.reconcileWorkspace](../../../src/Server/LazyLoadReconciliationServer.fs) (line 222). That walk runs on the Actor turn.
3. **File parse** — `parseFocusFile` (the call at line 133) calls [DocumentPersistWrite.fs](../../../src/Server/DocumentPersistWrite.fs) `planParseFile` on that same turn.

## What to build

`runLoad` does not call `reconcileWorkspace`. After the pull, the Load command sends a core message whose subject is that Workspace. The parse push is [CoreMailboxLoad.fs](../../../src/Server/Core/CoreMailboxLoad.fs) `markUnparsedThenPush`. The mailbox modules are [CoreMailboxEvents.fs](../../../src/Server/Core/CoreMailboxEvents.fs), [CoreMailboxActors.fs](../../../src/Server/Core/CoreMailboxActors.fs), and [CoreMailboxLoad.fs](../../../src/Server/Core/CoreMailboxLoad.fs). The mailbox posts Events. `parseFocusFile` stays as it is.

1. [ ] No inline reconcile — `continueLoad` does not call `reconcileWorkspace`. The function stays in its module.
2. [ ] Workspace post — git Load posts that Workspace id through `markUnparsedThenPush`. The post is one node.
3. [ ] File parse stays — This ticket does not change the `parseFocusFile` call or `planParseFile`.
4. [ ] Pull stays — The tracked-branch pull still runs.
5. [ ] Flag stays off — Do not set `requeueOnUnparsed` to true.
6. [ ] Test — A git Load posts that Workspace id and does not call `reconcileWorkspace`.
