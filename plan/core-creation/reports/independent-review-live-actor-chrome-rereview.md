# Independent re-review — live Actor chrome

Reviewer did not write the implementation. Implementer review files in the range were not read as authority. Ticket [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) Status stays `coded`. This report does not overwrite the earlier Needs-work file `independent-review-live-actor-chrome.md`.

**Range:** `origin/staging...HEAD` at `6af64ead4938d5f7cd2d12022340237d54e62dd0`. Base `origin/staging` `3221379be7ea0e727f8604a52bfac209570c2ed2`. Command: `git diff origin/staging...HEAD`. Diff is non-empty (32 files, +557 / −87).

**Spec:** [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) — active Actor chrome, Command start result, Poll/Command stop result, boot live chrome. [CONTEXT.md](CONTEXT.md) has Actor, Event, Poll, Sync, State, Focus, Run, Run Agent, Agent. There is no glossary term AI. Ticket start copy is **Run: AI started.** (wording may vary by Actor name).

**Commits** (`origin/staging..HEAD`):

- `6af64ead` Fix Fable record update after Command/Poll apply
- `f73b56d8` Convey ActorStop Succeeded on lastCmdResult
- `a2dc90cb` Apply Command events and seed live Actor chrome at boot
- `65bdbc40` Rename Client live-Focus set so Server pool accessors stay unique
- `4f55ff34` Add spec-axis review for 21 live Actor chrome.
- `df9e281f` Show live Actor chrome from Poll ActorStart and ActorStop

**Focused tests (green):** [ActorLiveTests](tests/Shared.Tests/ActorLiveTests.fs) — 4 passed. [ApiGetStateTests](tests/Server.Tests/ApiGetStateTests.fs) `getState seeds liveFocusIds from lockPresent overlay` — 1 passed.

## Prior must-fix (this tip)

Earlier Needs-work report `independent-review-live-actor-chrome.md` (tip `65bdbc40`) claimed three must-fix items. Checked on `6af64ead` against the ticket and code, not against implementer comments.

1. **Command POST applies returned Events — met.** Spec Sequence 1 Launch admits Actor: chrome on via `ActorStart` on the same Command response path; What to build 3 Start result: `lastCmdResult` shows **Run: AI started.** [App.fs](src/Client/App.fs) `runSubmitCommand` dispatches `CommandDone response.events`. [Update.fs](src/Client/Update.fs) `CommandDone` calls [applyCommandEvents](src/Client/UpdateActorLive.fs) → [SyncLogic.applyServerTail](src/Shared/SyncLogic.fs) and [ActorLive.lastCmdResult](src/Shared/ActorLive.fs) `Detail (Some "Run", "AI started.")`. Display is `Run: AI started.`
2. **Boot `/state` seeds live Focus chrome — met.** Spec What to build 5 Boot: after `/state` (or equivalent boot), a still-live Actor still shows chrome; prefer an existing Server surface; no Graph persist field and no new live-registry Poll. [Api.getState](src/Server/Api.fs) sets `seedLiveFocusIds` from [ActorLive.focusIdsFromLockPresent](src/Shared/ActorLive.fs) on the mailbox lockPresent overlay. [StateLoaded](src/Client/Update.fs) copies `response.seedLiveFocusIds` into `actorLiveFocusIds`. [BootGraphApplied](src/Client/Update.fs) copies `projectedLiveFocusIds` from the shared apply path.
3. **SyncLogic / Event-apply shared with Poll; ActorStop clears Failed and Cancelled — met.** Spec What to build 1 Projection: same Event-apply path as Graph Changes for Command, Poll, and Load; after `ActorStop` (Succeeded, Failed, or Cancelled) the Focus is not live. [foldProjectedEvents](src/Shared/SyncLogic.fs) / `withProjectedGraph` calls `ActorLive.applyEvent`. `EventBody.ActorStop(focusId, _)` removes the Focus for every ActorResult. Command uses `applyServerTail`; Poll and boot novel use `applyServerTail`; Load uses `applyLoadResponse` → `applySyncResponse`.

## Standards

Standards axis only. Range as pinned. Scan command: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` (`python` is not on PATH).

### Mechanical scan

```
src/Client/App.fs  .agents/rules/fsharp-source.md  FILE 818->824  already over 400 or new file over 400; change increased it
src/Client/RowView.fs  .agents/rules/fsharp-source.md  FILE 415->417  already over 400 or new file over 400; change increased it
src/Client/Update.fs  .agents/rules/fsharp-source.md  FILE 405->407  already over 400 or new file over 400; change increased it
src/Shared/ViewModel.fs  .agents/rules/fsharp-source.md  FILE 396->403  already over 400 or new file over 400; change increased it
tests/Shared.Tests/SerializationTests.fs  .agents/rules/fsharp-source.md  FILE 591->608  already over 400 or new file over 400; change increased it
tests/Shared.Tests/SyncLogicTests.fs  .agents/rules/fsharp-source.md  FILE 666->707  already over 400 or new file over 400; change increased it
tests/Shared.Tests/ViewModelRowStateTests.fs  .agents/rules/fsharp-source.md  FILE 791->820  already over 400 or new file over 400; change increased it
```

measure-fs-size: new bindings in [UpdateActorLive.fs](src/Client/UpdateActorLive.fs) and [ActorLive.fs](src/Shared/ActorLive.fs) are 2–15 lines (all under 40). [SyncLogic.fs](src/Shared/SyncLogic.fs) is 389 lines at this tip (no FILE hit). Tests are exempt from file size in [fsharp-source.md](.agents/rules/fsharp-source.md).

### Hard violations

1. **File size** — [fsharp-source.md](.agents/rules/fsharp-source.md): 800 lines or less; if a file is already longer, split when the change increases it (standalone later commit). Scan: [App.fs](src/Client/App.fs) 818→824 (already over 800; Command POST now dispatches `CommandDone` / `CommandFailed`). [RowView.fs](src/Client/RowView.fs) 415→417. [Update.fs](src/Client/Update.fs) 405→407. [ViewModel.fs](src/Shared/ViewModel.fs) 396→403 (crossed 400 for `AppliedBrowserGraph` and `CommandDone`).
2. **Labeled links** — [markdown-writing.md](.agents/rules/markdown-writing.md): labeled links are `[label](path)`, not Obsidian `[[path|label]]`. Rewritten hunks on [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) keep labeled wikilinks (Context `07` / `35b` / `09`; Non-goals `22`; See also line). [project.md](plan/core-creation/project.md) new notes use `[label](path)` correctly.
3. **Name wraps the id** — [refer-by-name.md](.agents/rules/refer-by-name.md): never refer by only the id. New See also label `[[plan/llm-connector/issues/09-agent-failure-preserves-children.md|09]]` is the id only. Other rewritten labels `|09`, `|35b`, `|22`, `|10` in the same ticket are id-only.

### BootGraphApplied tuple

Not a hard one-off-tuple hit on this tip. The prior 4- or 5-slot payload is now named [AppliedBrowserGraph](src/Shared/ViewModel.fs) (`projectedGraph`, `projectedEventId`, `projectedHistory`, `projectedLiveFocusIds`). Residual `BootGraphApplied of AppliedBrowserGraph * isReady: bool` is a labeled DU payload, same shape as `PollDone` / `LoadDone`. Folding `isReady` into `AppliedBrowserGraph` would mix boot-only ready with apply state (mega-record half of the same rule).

### Not hit

Functions ≤40. No new >100-character lines. No new mutable or Exceptions. Projection stays in Shared ([core-api.md](.agents/rules/core-api.md)). Public field is `actorLiveFocusIds`. CSS class is `amb-actor-live`. No ticket number in a new module name.

### Judgement calls ([SMELLS.md](.agents/skills/code-review/SMELLS.md))

- **Duplicated Code** — [ViewModelDomPlan.fs](src/Shared/ViewModelDomPlan.fs) hardcodes the class [RowView.fs](src/Client/RowView.fs) takes from `ActorLive.liveRowClass`: `CssClass.addIf (Set.contains entry.nodeId model.actorLiveFocusIds) "amb-actor-live"`
- **Divergent Change** — general `ClientSyncState` now lives in [ActorLive.fs](src/Shared/ActorLive.fs).
- **Shotgun Surgery** — `actorLiveFocusIds` is threaded through VM, sync state, `StateResponse`, boot cache, and both row paths (expected for this field).

Standards axis: 3 hard, 3 judgement. Worst hard: [App.fs](src/Client/App.fs) grown while already over 800 lines.

## Spec

Spec: ticket Sequence + What to build. [CONTEXT.md](CONTEXT.md) Run / Run Agent / Agent when wording is in scope.

### (a) Missing or partial

1. **Cache truncate zeros the live seed.** Spec What to build 5 Boot: “After `/state` (or equivalent boot), a still-live Actor still shows chrome.” [requestIdleTruncate](src/Client/BootCacheStore.fs) writes `seedLiveFocusIds = Set.empty`. After truncate, ActorStart Events that were folded into the snapshot are gone, so a later cache replay cannot rebuild the live set. Fresh `/state` is seeded. Current [Program.fs](src/Client/Program.fs) first paint is `loadFromState` (not cache-first).

### (b) Scope creep

1. **Stop copy uses command name Ask.** [ActorLive.lastCmdResult](src/Shared/ActorLive.fs) stop arms are `Detail (Some "Ask", "Actor succeeded.")` and `Error (Some "Ask", …)`. Spec What to build 4 Stop / error result asked for a success detail and an Error or clear status from the stop — not the name Ask. [CONTEXT.md](CONTEXT.md) **Run Agent** avoids Ask as the command name. Start copy correctly uses Run. [RunAgentActor](src/Server/RunAgentActor.fs) already sets `DisplayName = Some "Ask"` (not introduced by this range).

### (c) Looks implemented but wrong

None on the three prior must-fix paths. Command apply, `/state` seed, shared fold, and ActorStop(_, _) match the quoted spec lines. Chrome is `amb-actor-live` inset on the row (spec: row / outline). No Graph persist lock-present field. No Cancel UI. No new live-registry Poll.

## Summary

Standards: 3 hard (file size, labeled wikilinks, id-only labels); worst: [App.fs](src/Client/App.fs) 818→824 while already over 800. Spec: 1 partial (cache truncate seed), 1 wording note (Ask on stop); worst: cache truncate can drop live ids on a later cache boot. Prior must-fix 1–3 are met on this tip.

**Verdict:** Good
