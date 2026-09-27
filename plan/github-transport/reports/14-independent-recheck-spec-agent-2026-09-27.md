# Independent recheck — [14 — Route Load and Save by path pre-pick](plan/github-transport/issues/14-route-load-save-by-pre-pick.md)

Range `630f763e^...HEAD` (commit `630f763e`). Spec: [14 — Route Load and Save by path pre-pick](plan/github-transport/issues/14-route-load-save-by-pre-pick.md). Recheck of must-fixes **3.1**, **3.2**, and **3.3** in [14-independent-code-review-2026-09-27.md](14-independent-code-review-2026-09-27.md). Source: production wiring and tests. Focused `LoadSaveCommand` tests: 5 passed, 0 skipped.

## 1. Must-fix — Desk Load/Save preservation proof

Ticket item **1.3.3 Preserve the desk path** still lacks a test that reaches actual WebDAV Workspace Upload or `/{file}/save`. Production wiring exists. The new tests stop at Effects.

1. **1.1. Desk Load stops before WebDAV** — [LoadSaveCommandClientTests.fs](tests/Server.Tests/LoadSaveCommandClientTests.fs) `Desk Load response continues to mapped workspace push` does send the Desk response through [LoadSaveCommandClient.fs](src/Client/LoadSaveCommandClient.fs) `runWith` and [continueDesk](src/Client/LoadSaveCommandClient.fs) into [deskLoadOp](src/Client/UpdateWorkspaceLoad.fs). The assert is `ContinueWorkspaceStubsThenPush`. [App.fs](src/Client/App.fs) maps that Effect to `POST /_desktop/workspace-inventory`, not to WebDAV. WebDAV is `ContinueWorkspacePush` → `POST /_desktop/workspace-push` after [completeUploadInventory](src/Client/UpdateWorkspaceSync.fs). This test never runs that hop.
2. **1.2. Desk Save URL check is a false-positive** — `Desk Save response continues to existing save endpoint` proves `deskSaveOp` returns `ContinueDeskSave`. It then asserts `UpdateSave.deskSaveUrl "ambit" = "/ambit/save"` with no call to [runDeskSave](src/Client/UpdateSave.fs) and no `POST`. [App.fs](src/Client/App.fs) is the only production link (`ContinueDeskSave` → `runDeskSave` → `/{file}/save`). That link is untested.
3. **1.3. Desk Load mapping source changed** — [deskLoadOp](src/Client/UpdateWorkspaceLoad.fs) `hasMapping` no longer calls `lookupMappedPath` (`GET /_desktop/workspace-mappings`). It reads `workspaceMappedLabels` through `canCompareWorkspacePathSync`. The Load test sets that set. That is not the prior live mapping lookup that starts WebDAV Upload.

## 2. Good — Routed Git Save and Load proof

1. **2.1. Git Save through the command door** — `routed Git Save commits then pushes through Peer Actor` posts through [Api.postLoadSaveCommand](src/Server/Api.fs) and [CoreMailbox.startLoadSaveCommand](src/Server/Core/CoreMailbox.fs) with [GithubTransportActor.productionDependencies](src/Server/GithubTransportActor.fs). After `ActorSucceeded`, a clone of the remote has `saved.txt` and matching `refs/heads/main`. This is not the direct [GithubTransportActor.start](src/Server/GithubTransportActor.fs) seam in [GithubTransportActorTests.fs](tests/Server.Tests/GithubTransportActorTests.fs).
2. **2.2. Git Load pull, Parse, Poll** — `routed Git Load pulls and Poll sees Parse lifecycle` uses `LoadSavePrePick.Plain` on a tracked Workspace. [LoadSaveRouting.resolvePath](src/Server/LoadSaveRouting.fs) returns Git. The work tree receives the pulled file. [Api.postPoll](src/Server/Api.fs) then contains `ActorStart`, `ActorStop`, and a `Parse` `Change` from [LazyLoadReconciliationServer.reconcileWorkspace](src/Server/LazyLoadReconciliationServer.fs) (`GraphOnlyChangePost` command name `"Parse"`).

## 3. Good — Test function size

The 55-line `Git request reaches Peer Actor through mailbox and actor pool` function is gone. Measured spans in the range are all ≤40. Largest: `Desk request reaches pool without starting Peer Actor` at 39 lines. New Git tests are 24 and 29 lines. [LoadSaveCommandClient.fs](src/Client/LoadSaveCommandClient.fs) `runWith` is 24 lines.

## 4. False-positive tests

1. **4.1. Desk Save endpoint** — `Desk Save response continues to existing save endpoint` does not traverse `/{file}/save`. The URL helper is a separate assert.
2. **4.2. Desk Load workspace push** — `Desk Load response continues to mapped workspace push` does not traverse `/_desktop/workspace-push`. It asserts the inventory Effect only.
3. **4.3. Git tests are not false-positives** — Both Git tests went through the command-request door to real git work and passed (not skipped).

## 5. Summary

Must-fix 1 remains Must-fix. Must-fix 2 is Good. Must-fix 3 is Good. Worst remaining issue: Desk Load/Save tests do not reach WebDAV Workspace Upload or `/{file}/save`.
