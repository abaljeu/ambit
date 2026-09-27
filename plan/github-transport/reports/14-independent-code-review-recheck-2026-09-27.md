# Independent code review recheck — [14 — Route Load and Save by path pre-pick](plan/github-transport/issues/14-route-load-save-by-pre-pick.md)

Range: `630f763e^...HEAD`. Recheck of the three Must-fixes in [14-independent-code-review-2026-09-27.md](plan/github-transport/reports/14-independent-code-review-2026-09-27.md). This review is not approval. Ticket Status stays `coded`.

## 1. Must-fix — Desk preservation proof remains incomplete

[LoadSaveCommandClientTests.fs](tests/Server.Tests/LoadSaveCommandClientTests.fs) now passes Desk responses through `LoadSaveCommandClient.runWith`, but both tests stop at intermediate Effects. Desk Load asserts `ContinueWorkspaceStubsThenPush`; it does not continue through inventory to observe the WebDAV `/_desktop/workspace-push` request. Desk Save asserts `ContinueDeskSave` and separately checks `deskSaveUrl`; it does not execute the Effect and observe `POST /ambit/save`. The production links exist in [App.fs](src/Client/App.fs), but the required behavior proof remains compositional.

## 2. Good — Routed Git Save and Load

[LoadSaveCommandTests.fs](tests/Server.Tests/LoadSaveCommandTests.fs) now routes Git Save through `Api.postLoadSaveCommand`, the mailbox, the actor pool, and the production Peer Actor; it then proves the pushed remote contains the saved file. Routed Plain Load selects Git for a Workspace with a remote, pulls the remote file, reaches Parse reconciliation, and proves Poll-visible ActorStart, successful ActorStop, and Parse Change Events.

## 3. Good — Test function size

The prior 55-line test is gone. Manual measurement of the changed test bindings finds no function over 40 lines; the largest is the 39-line Desk pool test. The mechanical production scan also reports no changed function over 40 lines.

## 4. Summary

Must-fix 1 remains Must-fix. Must-fixes 2 and 3 are Good. The ticket correctly remains `coded`.
