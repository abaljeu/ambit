# 22 — Drop the Browser git Load after-step

**Status:** `defined`
**Type:** coding
**Blocked by:** [21 — Git Load posts the Workspace](21-git-load-posts-workspace.md)
**Trial step:** 4. Next: [24 — Load and Save on the events list](24-load-save-on-events.md).

**Binding arch:** [github-transport architecture](../arch.md), [core-refinement architecture](../../core-refinement/arch.md), [parse-thread architecture](../../parse-thread/arch.md).

## Context

Trial step 4. After git Load returns, the Browser runs a second reconcile or parse. That after-step goes. The Actor already posted the Workspace in [21 — Git Load posts the Workspace](21-git-load-posts-workspace.md).

Alan, 2026-10-09. Load from the Browser must not trigger a client reconcile function.

## Current state

1. **Response** — [LoadSaveCommandClient.fs](../../../src/Client/LoadSaveCommandClient.fs) `applyResponse` (line 27). The git Load branch at line 39 calls `continueGitLoad`.
2. **After-step** — `continueGitLoad` (line 76) dispatches [gitLoadAfterOp](../../../src/Client/UpdateWorkspaceLoad.fs) (line 56).
3. **File** — `gitLoadAfterOp` calls `parseFocusedFile` (line 46), which calls `parseFileOp`.
4. **Directory** — The other branch calls `startDirectoryReconcile` (line 31), which returns `Effect.ContinueDirectoryReconcile`.
5. **Save** — The Save branch at line 40 already returns `()`.

Desktop push still calls `ContinueDirectoryReconcile` from [UpdateWorkspaceSync.fs](../../../src/Client/UpdateWorkspaceSync.fs). That caller stays until [26 — Revisit WebDAV parse](26-revisit-webdav-parse.md).

## What to build

The git Load branch returns `()`. Delete the after-step functions when nothing calls them.

1. [ ] Load branch — `applyResponse` for `LoadSaveOperation.Load` returns `()`. It does not call `continueGitLoad`.
2. [ ] Delete the dispatcher — Delete `continueGitLoad` when nothing calls it.
3. [ ] Delete the after-step — Delete `gitLoadAfterOp` when nothing calls it.
4. [ ] Delete the helper — Delete `startDirectoryReconcile` when nothing calls it. Leave `Effect.ContinueDirectoryReconcile` in place while desktop push still emits it.
5. [ ] Test — A git Load response does not dispatch `gitLoadAfterOp`. `continueGitLoad` and `gitLoadAfterOp` are absent. `startDirectoryReconcile` is absent when it has no caller.
