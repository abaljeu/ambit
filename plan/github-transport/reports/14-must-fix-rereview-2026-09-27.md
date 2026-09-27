# 14 — Must-fix independent re-review (2026-09-27)

## 1. Must-fix 1 remains open — Desk continuation proof is compositional

[LoadSaveCommandClientTests.fs](tests/Server.Tests/LoadSaveCommandClientTests.fs) passes each Desk response through `LoadSaveCommandClient.runWith`, but `runDesk` replaces the production `continueDesk` mapping with `continued.Add`. The Load fact then calls `WorkspaceUpload.plan` directly, separate from the captured continuation. It does not prove that the response selects `deskLoadOp` or that the selected operation produces `DesktopPush`. The Save fact likewise calls `UpdateSave.deskSaveUrl "ambit"` directly. It proves the helper returns `/ambit/save`, but it does not prove that the response selects and executes `deskSaveOp`. The two facts pass 2/2, but their names overstate the covered integration.

## 2. False confidence — Poll Parse assertion does not identify a Change

The routed Plain-with-remote Load fact in [LoadSaveCommandTests.fs](tests/Server.Tests/LoadSaveCommandTests.fs) crosses `Api.postLoadSaveCommand` → mailbox → pool → the real production `GithubTransportActor`, pulls the remote file, and reads ActorStart, ActorStop, and Parse through `Api.postPoll`; the routed suite passes 3/3 with no git skips. However, its Parse predicate checks only `event.commandName = "Parse"`. It does not also require `EventBody.Change`, so the fact is weaker than the intended Parse-hop proof and could accept a future non-Change Event with that command name.

## 3. Summary — Finding count

2 findings: 1 unresolved Must-fix and 1 false-confidence gap. No finding applies to routed Save/Load production transport or the 40-line limit: Save verifies the pushed remote, Plain Load verifies pulled bytes and Poll-visible lifecycle, every changed helper or fact in `LoadSaveCommandTests.fs` is at most 31 lines, and the former 55-line fact is removed.
