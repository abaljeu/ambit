# Independent recheck — Standards — [14 — Route Load and Save by path pre-pick](plan/github-transport/issues/14-route-load-save-by-pre-pick.md)

Range: `630f763e^...HEAD` (`630f763e` Add Load and Save routing behavior coverage). Sources: [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md), [.agents/rules/core-agent-behavior.md](.agents/rules/core-agent-behavior.md), [.agents/skills/code-review/SMELLS.md](.agents/skills/code-review/SMELLS.md), [14-independent-code-review-2026-09-27.md](14-independent-code-review-2026-09-27.md). This review is not approval.

## 3.1 Desk preservation — Good

[LoadSaveCommandClientTests.fs](tests/Server.Tests/LoadSaveCommandClientTests.fs) posts a Desk response through [LoadSaveCommandClient.fs](src/Client/LoadSaveCommandClient.fs) `runWith` with production `continueDesk`. Desk Load applies `deskLoadOp` and gets `ContinueWorkspaceStubsThenPush` for mapped Workspace `home`. Desk Save applies `deskSaveOp` and gets `ContinueDeskSave`; `deskSaveUrl` is `/{file}/save`. [App.fs](src/Client/App.fs) runs `runDeskSave` on that Effect. Ticket item 1.3.3 now has Client-door proof for both operations.

## 3.2 Routed Git matrix — Good

[LoadSaveCommandTests.fs](tests/Server.Tests/LoadSaveCommandTests.fs) sends both operations through `Api.postLoadSaveCommand` and `LoadSaveRouting.resolvePath` into production Peer Actors. `routed Git Save commits then pushes through Peer Actor` uses explicit Git, waits for `ActorSucceeded`, and proves the commit is on the remote. `routed Git Load pulls and Poll sees Parse lifecycle` uses Plain on a Workspace with a remote (path is Git), proves the pulled file, and Poll contains ActorStart, ActorStop, and a Parse Change from `LazyLoadReconciliationServer.reconcileWorkspace`. The old stub Git Load test is gone.

## 3.3 Function size — Good

The 55-line `Git request reaches Peer Actor through mailbox and actor pool` binding is gone. Manual measure of every new test and helper `let` (scanner skips `tests/`): largest are `runDesk` 30 lines, `seedWorkspace` 28, `routed Git Load…` 28, `routed Git Save…` 23, Desk pool test 38. No binding exceeds 40 lines. No added test line exceeds 100 characters. Given production scan lines are all at or below the limit (`runWith` 24, `runDeskSave` 22); none is a finding. [fsharp-source.md](.agents/rules/fsharp-source.md) “40 lines or less per function” holds.

## Summary

Standards: 0 Must-fix. Spec (the two proof items): 0 Must-fix. All three prior must-fixes are Good.
