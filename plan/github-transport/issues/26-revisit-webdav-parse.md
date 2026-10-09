# 26 — Revisit WebDAV parse

**Status:** `needs-info`
**Type:** grilling
**Blocked by:** [27 — Field test the Load trial sequence](27-field-test-the-load-trial-sequence.md)
**Trial step:** 8.

**Binding arch:** [github-transport architecture](../arch.md), [core-refinement architecture](../../core-refinement/arch.md), [parse-thread architecture](../../parse-thread/arch.md).

## Context

Trial step 8. Server-side paced parsing replaces the client Desktop-sourced parse. The WebDAV transit stays. What goes is the Browser path that reads a desktop file and asks the server to parse it.

Alan, 2026-10-09. The server that stored the bytes posts that node to the parse stack. [25 — Pace server functions](25-pace-server-functions.md) spaces that work. The Browser does not source the parse text.

## Recommendation

The server posts the node after the bytes are stored. The parse stack parses it. `DesktopPush`, `startWorkspacePush`, and `desktopReadPath` stop feeding parse.

## Questions

1. **Which callers go?** `startWorkspacePush` ([UpdateWorkspaceSync.fs](../../../src/Client/UpdateWorkspaceSync.fs) line 177), `desktopReadPath` ([WorkspaceUpload.fs](../../../src/Shared/WorkspaceUpload.fs) line 50), and `ContinueParseFile` ([UpdateImport.fs](../../../src/Client/UpdateImport.fs) line 116, read at [App.fs](../../../src/Client/App.fs) line 244). Do all three go, or does one stay for a mapped desktop that has not uploaded yet? Recommendation: all three stop feeding parse. A mapped desktop uploads first. The server parses after the bytes are stored.
2. **Who posts?** [WorkspaceWebDav.fs](../../../src/Server/WorkspaceWebDav.fs) receives the upload. Does that handler post the File id to the parse stack, or does a later server step post it? Recommendation: the handler that stored the bytes posts that File id. One post. [25 — Pace server functions](25-pace-server-functions.md) spaces the parse.
3. **Directory after upload?** A desktop Load of a Directory today pushes the tree, then reconciles. After this change, does the server post that Directory id once, and the Browser stay quiet? Recommendation: the server posts that Directory id once. The Browser does not reconcile it.

## Research

1. **Desktop push** — [UpdateWorkspaceLoad.fs](../../../src/Client/UpdateWorkspaceLoad.fs) line 76 matches `DesktopPush` and calls `startWorkspacePush`.
2. **Queued push** — [App.fs](../../../src/Client/App.fs) line 102 runs `QueuedWorkspacePush` through `startWorkspacePush`.
3. **Desktop text** — `desktopReadPath` returns the desktop path only for `DesktopPush`. [App.fs](../../../src/Client/App.fs) line 244 reads that path and sends the text to `POST /ambit/file/parse`.
4. **WebDAV** — [RouteRegistration.fs](../../../src/Server/RouteRegistration.fs) line 508 calls `WorkspaceWebDav.registerRoutes`. That module stores bytes. It does not post the parse stack.
