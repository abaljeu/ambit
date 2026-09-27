# Independent code review recheck 2 — [14 — Route Load and Save by path pre-pick](../issues/14-route-load-save-by-pre-pick.md)

Range: `e8a46fff^...HEAD`. This recheck verifies the two prior Must-fixes and classifies the attached final Server-suite failure. Ticket Status stays `coded`.

## Landing decision

**Fail for landing.** Routed Git Save is stable in the full Server suite, but the Desk preservation Must-fix is not closed because the new tests observe injected delegate calls rather than real HTTP requests.

## Must-fix

### 1. Desk preservation still lacks real end-to-end HTTP proof

[LoadSaveCommandClientTests.fs](../../../tests/Server.Tests/LoadSaveCommandClientTests.fs) now follows `ContinueWorkspaceStubsThenPush` through `ContinueWorkspacePush` and follows `ContinueDeskSave` into `DeskLoadSaveEffectClient.runDeskSaveWith`. However, `effectDependencies` supplies fake `postJson` and `postEmpty` delegates that only append URL strings to a `ResizeArray`. No HTTP handler, `HttpClient`, Server endpoint, WebDAV request, or actual POST runs. Assertions on `"/_desktop/workspace-push"` and `"/ambit/save"` therefore prove calls to an injected seam, not real end-to-end HTTP. The prior Must-fix remains open.

Required closure: an equivalent test must drive the Desk continuations through HTTP and observe WebDAV `/_desktop/workspace-push` plus the actual Desk Save POST at `/ambit/save` or `/{file}/save`.

## Should-fix

### 1. Classify the attached WebDAV failure as a pre-existing suite flake

The attached run failed 1/558 in `WorkspaceWebDavTests.WorkspaceFileSync.post uploads edited local file into DataDir` with `fatal: cannot lock ref 'HEAD': reference already exists`. This is not a Ticket 14 Must-fix and is not a regression from `e8a46fff`.

The failure has a pre-existing race: the test starts the real Server over a dirty child repository; `DailyGitSave.register` starts an asynchronous application-start commit, while WebDAV `handlePrepare` can run another JIT commit on the same repository. `DailyGitSave.commitRoot` does not use `WorkspaceGit.withWorkTreeGate`. The WebDAV test, WebDAV handler, daily-save implementation, and `ensureInit` path are unchanged by this fix. The current branch passed that test both in the complete suite and alone, which is consistent with a timing-dependent race. Track and fix this outside Ticket 14.

### 2. Remove the wrapper orphaned by the fix

[UpdateWorkspaceSync.fs](../../../src/Client/UpdateWorkspaceSync.fs) defines `completeUploadInventory` as a wrapper around `completeUploadInventoryWith`, but no caller remains. The extraction changed the production call to `completeUploadInventoryWith`, so the wrapper is an orphan created by this commit.

## Good

### 1. Routed Git Save Must-fix is closed

[LoadSaveCommandTests.fs](../../../tests/Server.Tests/LoadSaveCommandTests.fs) replaces the fixed `waitForStop ... 2000` countdown with a 30-second deadline and bounded exponential polling delay. The routed Save test still clones the pushed bare remote, asserts `saved.txt` exists there, and compares the remote and work-tree `main` refs.

### 2. Full Server-suite stability is proven on this branch

The complete built Server suite passed 558/558 in 2m09s. The focused Load/Save set passed 5/5, including routed Git Save in 289 ms. The previously failing WebDAV test also passed alone in 433 ms. The focused results support, but do not replace, the complete-suite result.

### 3. Ticket Status is correct

[14 — Route Load and Save by path pre-pick](../issues/14-route-load-save-by-pre-pick.md) remains `coded`.

## Standards

One hard surgical-change finding: the fix leaves the unused `completeUploadInventory` wrapper. The mechanical F# scan reports no function-size violation. Dependency-record cohesion and duplicated callback observations are judgment calls and do not change the landing decision.

## Spec

One finding: the Desk preservation proof remains partial because it stops at injected HTTP delegates. Routed Git Save has no remaining spec finding. The attached WebDAV failure is an unrelated pre-existing race, not a Ticket 14 regression.

Summary: Standards has 1 finding, worst is the orphaned wrapper. Spec has 1 finding, worst is missing real HTTP Desk preservation proof. Landing decision: Fail.
