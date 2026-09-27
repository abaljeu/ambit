# Independent code review recheck 3 — [14 — Route Load and Save by path pre-pick](../issues/14-route-load-save-by-pre-pick.md)

Range: implementer commit `4f01a5c7` on PR 141. This recheck verifies the Desk HTTP proof, Routed Git Save proof, orphan removal, and attached Server-suite result. Ticket Status stays `coded`.

## 1. Landing decision — Pass

**Pass for landing.** The Desk preservation Must-fix is closed. The tests drive the Desk continuations through `HttpClient.PostAsync` into `HttpMessageHandler.SendAsync` and observe `POST /_desktop/workspace-push` plus `POST /ambit/save`. This is sufficient handler-backed HTTP request proof under the stated acceptance rule. It is not the prior seam-only fake that recorded calls made directly to bare `postJson` and `postEmpty` delegates. The handler returns canned responses and does not start a real App or Server host, so the proof is transport-level rather than a live-host end-to-end test.

## 2. Must-fix — None

No Must-fix remains for landing.

## 3. Should-fix

### 3.1. Remove the unused request-body binding

[LoadSaveCommandClientTests.fs](../../../tests/Server.Tests/LoadSaveCommandClientTests.fs) reads the request content into `body` in `DeskHttpHandler.Handle` but never uses that binding. This test-only cleanup does not weaken the observed HTTP method and path, so it does not block landing.

### 3.2. Return status and response body from one path match

`DeskHttpHandler.Handle` lists the same three accepted paths once to choose a response body and again to choose `HttpStatusCode.OK`. One match that returns both values would remove this small duplicated-code smell. This does not affect the proof or landing decision.

## 4. Good

### 4.1. Desk Load reaches the WebDAV push request through HTTP

[LoadSaveCommandClientTests.fs](../../../tests/Server.Tests/LoadSaveCommandClientTests.fs) starts from the Desk Load command response, applies the production Desk continuation mapping, follows `ContinueWorkspaceStubsThenPush` through inventory completion to `ContinueWorkspacePush`, and calls the extracted effect interpreter. `postHttp` then calls `HttpClient.PostAsync`; `DeskHttpHandler.SendAsync` observes `POST /_desktop/workspace-push`.

### 4.2. Desk Save reaches the existing Save request through HTTP

The Desk Save test starts from the Desk Save command response, applies the selected updater, follows `ContinueDeskSave`, and runs `DeskLoadSaveEffectClient.runDeskSaveWith`. That path uses `UpdateSave.deskSaveUrl` and `HttpClient.PostAsync`; the handler observes `POST /ambit/save`, which is the accepted `/{file}/save` form for file name `ambit`.

### 4.3. Routed Git Save proof remains intact

[LoadSaveCommandTests.fs](../../../tests/Server.Tests/LoadSaveCommandTests.fs) still uses a 30-second deadline with bounded adaptive polling for ActorStop. The routed Git Save test still clones the bare remote, asserts that `saved.txt` exists in that clone, and compares the remote clone and work-tree `main` refs.

### 4.4. The orphaned inventory wrapper stays removed

The repository contains `completeUploadInventoryWith` and its caller, but no `completeUploadInventory` wrapper.

### 4.5. Suite evidence is consistent and independently spot-checked

The attached log records a successful Server build with 0 warnings and 0 errors, followed by 558/558 Server tests passing in 2m02s and process `exit_code=0`. This recheck also built the Server test project successfully and directly ran the focused Load/Save filter: 5/5 passed in 1.26s, including both Desk HTTP proofs and Routed Git Save.

### 4.6. Ticket Status remains correct

[14 — Route Load and Save by path pre-pick](../issues/14-route-load-save-by-pre-pick.md) remains `coded`.

## 5. Review-axis summary

Standards has two non-blocking test-code findings: one unused binding and one duplicated path match. Spec has no finding. Worst Standards issue: unused test-only `body` binding. Landing decision: Pass.
